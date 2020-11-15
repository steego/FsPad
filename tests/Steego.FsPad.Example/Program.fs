// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System

// Define a function to construct a message to print
let from whom =
    sprintf "from %s" whom

open FsPad
let browser = FsPad.Web(8080)
let dump(value) = browser.Dump(value)

// let toJson object = Newtonsoft.Json.JsonConvert.SerializeObject(object)

type TaggedType(tag: string) = 
    member this.Tag = tag

type Person(name: string, age:int) = 
    inherit TaggedType("Person")
    member this.Name = name
    member this.Age = age

let doMain = async {
    for x in 1..10000 do
        let message = sprintf "Hello %i" x
        let object = {| Message = message; X = x |}
        let bob = Person("Bob", 30)
        dump(
            {| 
                StringValue = "one"
                Num = 1
                Value = object
                Json = object
                Bob = bob 
                People = [
                    {| Name = "Jim"; Age = 20 |}
                    {| Name = "Larry"; Age = 20 |}
                ]
            |}
        )
        do! Async.Sleep(1000)
}

[<EntryPoint>]
let main argv =

    Async.RunSynchronously doMain

    // let message = from "F#" // Call the function
    // printfn "Hello world %s" message
    0 // return an integer exit code