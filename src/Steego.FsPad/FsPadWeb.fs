module FsPad.Web

open SocketServer
open ReflectionPrinter


let server = startServer(8080)

let dumpLevel (level:int) (value:'a) = print level value |> PrettyPrinter.render |> server.SendToAll
let dump x = dumpLevel 3 x