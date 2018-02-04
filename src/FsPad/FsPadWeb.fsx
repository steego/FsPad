#load "SocketServer.fsx" //  ignore-cat
#load "ReflPrinter.fsx" // ignore-cat

open SocketServer
open ReflPrinter

let server = startServer(8080)

let dumpLevel (level:int) (value:'a) = 
    let html = Printer.Print(value, level)
    server.SendToAll(html)

type Results() =
    static member Dump(objValue) = Results.Dump(objValue, 3)
    static member Dump(objValue, maxLevel : int) =
        let html = Printer.Print(objValue, maxLevel)
        server.SendToAll(html)
let dump x = Results.Dump x