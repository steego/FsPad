namespace FsPad

open SocketServer
open ReflectionPrinter

type Web(port:int) = 
    let server = startServer(port)

    ///  Displays the content of an object with a maximum depth
    let dumpLevel (level:int) (value:'a) = 
        value |> print level |> PrettyPrinter.render |> server.SendToAll

    let dump x = dumpLevel 3 x

    member this.OpenBrowser() = server.OpenBrowser()
    member this.OpenBrowserIfDisconnected() = server.OpenBrowserIfDisconnected()
    member this.Dump(value) = dump(value)
    member this.Dump(value, level) = dumpLevel level value