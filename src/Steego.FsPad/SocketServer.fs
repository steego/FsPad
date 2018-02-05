module FsPad.SocketServer

open System.Threading.Tasks
open Suave
open Suave.Http
open Suave.Operators
open Suave.Filters
open Suave.Successful
open Suave.Files
open Suave.RequestErrors
open Suave.Logging
open Suave.Utils
open System
open System.Net
open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket
open System.Collections.Concurrent
open System.IO

type Id = string

module Log =
    let getDate() = 
        let now = DateTime.Now
        sprintf "%02i:%02i:%02i" (now.Hour) (now.Minute) (now.Second)
    let debug(msg) = printfn "[%s DBG] %s" (getDate()) msg

module Common = 

    let private indexContent = 
        let assm = System.Reflection.Assembly.GetExecutingAssembly()
        let resourceName = "index.html"
        use stream = assm.GetManifestResourceStream(resourceName)
        use reader = new StreamReader(stream)
        reader.ReadToEnd()

    /// Sends text to a websocket
    let private sendText (webSocket : WebSocket) (response : string) = 
        (let byteResponse = 
             response
             |> System.Text.Encoding.ASCII.GetBytes
             |> ByteSegment
         // the `send` function sends a message back to the client
         webSocket.send Text byteResponse true)
    
    let private (|Message|_|) msg = 
        match msg with
        | (Text, data, true) -> Some(System.Text.Encoding.UTF8.GetString(data))
        | _ -> None
    
    type IConnection = 
        abstract Id : string
        abstract OnReceive : IObservable<Message>
        abstract SendAsync : string -> Task
    
    and Message = 
        { Connection : IConnection
          Message : string }
    
    type private SuaveConnection(context : HttpContext, ws : WebSocket) = 
        let id = Guid.NewGuid().ToString()
        let onReceive = new Event<Message>()
        member this.Id = id
        member this.TellReceived(msg) = onReceive.Trigger(msg)
        member this.OnReceive = onReceive.Publish :> IObservable<_>
        member this.SendAsync(msg : string) = async { let! _ = msg |> sendText ws
                                                      () } 
        interface IConnection with
            member this.Id = id
            member this.OnReceive = this.OnReceive
            member this.SendAsync(msg) = 
                this.SendAsync(msg) |> Async.StartAsTask :> Task
    
    type Server(defaultConfig : SuaveConfig) = 
        let mutable started = false
        let connections = ConcurrentDictionary<string, SuaveConnection>()
        let onReceive = new Event<Message>()
        
        let getConn (id : string) = 
            let (exists, conn) = connections.TryGetValue(id)
            conn :> IConnection
        
        let socketHandler (ws : WebSocket) (context : HttpContext) = 
            socket { 
                let mutable loop = true
                let conn = SuaveConnection(context, ws)
                let id = conn.Id
                connections.AddOrUpdate(id, conn, fun x y -> conn) |> ignore
                while loop do
                    let! msg = ws.read()
                    match msg with
                    | Message(str) -> 
                        onReceive.Trigger({ Connection = getConn (id)
                                            Message = str })
                    | (Close, _, _) -> 
                        let _ = connections.TryRemove(id)
                        loop <- false
                    | _ -> ()
            }

        let getContent() = 
            let myPort = sprintf "%i" (defaultConfig.bindings.Head.socketBinding.port)
            indexContent.Replace("$$$PORT$$$", myPort).Replace("$$$CONTENT$$$", "Hello!")

        
        let mutable app = 
            choose [ path "/websocket" >=> handShake socketHandler
                     GET >=> OK(getContent())
                     NOT_FOUND "Found no handlers." ]
        
        let start() = 
            if not started then
                Log.debug("Started Webserver!")
                lock connections (fun () -> started <- true)
                let config = { defaultConfig with logger = Targets.create Verbose [||] }
                let app (ctx : HttpContext) = async { return! (lock connections (fun () -> app)) ctx }
                Async.Start(async { startWebServer config app })
                Log.debug "Web server started"
        
        member this.Start() = start()
        
        member this.OnReceived = onReceive.Publish
        
        member this.Connections = 
            connections
            |> Seq.map (fun c -> c.Value :> IConnection)
            |> Seq.toArray

        member this.OpenBrowserIfDisconnected() = 
            if this.Connections.Length < 0 then
                this.OpenBrowser()

        member this.OpenBrowser() = 
            let uri = defaultConfig.bindings.Head.uri "" ""
            printfn "Uri: %s" (uri.ToString())
            System.Diagnostics.Process.Start(uri.ToString()) |> ignore

        member this.SendToAll(msg) =             
            Async.RunSynchronously <| async {
                for c in this.Connections do
                    do! c.SendAsync(msg) |> Async.AwaitTask
            }
                    

let private servers = System.Collections.Concurrent.ConcurrentDictionary<int, Common.Server>()

let private createServer (port : int) = 
    let config = { defaultConfig with bindings = [ HttpBinding.createSimple HTTP "127.0.0.1" port ] }
    let server = Common.Server(config)
    server

///**Creates or fetches and instances of a web server at the specified port**
///
///**Parameters**
///  * `port` - parameter of type `int`
///
///**Output Type**
///  * `Common.Server`
///
///**Exceptions**
///
let getServer (port) = servers.GetOrAdd(port, fun port -> createServer (port))

let startServer(port) = 
    let server = getServer(port)
    server.Start()
    server