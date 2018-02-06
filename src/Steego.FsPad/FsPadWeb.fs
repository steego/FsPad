namespace FsPad

open System
open SocketServer
open ReflectionPrinter

type Web(port:int) = 
    let server = startServer(port)

    let render level value = value |> print level |> PrettyPrinter.render

    let mutable myLookup : Map<string list, obj> = Map.empty

    let split(s:string) = s.Split('/') |> List.ofArray |> List.filter(fun s -> not(String.IsNullOrWhiteSpace(s)))

    let myFetch(ctx:Suave.Http.HttpContext) =
        let path = ctx.request.path |> split
        match myLookup |> Map.tryFind path with
        | Some(o) -> o |> render 3
        | None -> "Nothing set here"

    do server.SetContentFetch(myFetch)

    ///  Displays the content of an object with a maximum depth
    let dumpLevel (level:int) (value:'a) = 
        value |> print level |> PrettyPrinter.render |> server.SendToAll

    let dump x = dumpLevel 3 x

    member this.OpenBrowser() = server.OpenBrowser()
    member this.OpenBrowserIfDisconnected() = server.OpenBrowserIfDisconnected()
    member this.Dump(value) = dump(value)
    member this.Dump(value, level) = dumpLevel level value
    member this.DumpToPath(value, path) = this.DumpToPath(value, path, 3)
    member this.DumpToPath(value:'a, path, level) = 
        Async.RunSynchronously <| async {
            for c in server.Connections do
                let path = path |> split
                if c.Path = path then
                    myLookup <- myLookup |> Map.add path (value :> obj)
                    let value = (c.Path, path, value)
                    let html = value |> render level
                    do! c.SendAsync(html) |> Async.AwaitTask
        }        