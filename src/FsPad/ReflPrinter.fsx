#if INTERACTIVE
#load "PrettyPrinter.fsx"
#load "Reflection.fsx"
#endif

open System
open PrettyPrinter

open Reflection.TypePatterns
open Reflection.ObjectPatterns

let rec print (level:int) (o:obj) : PrettyPrint = 
    match o with
    | null                      -> Value(null, null)
    | :? unit                   -> Value("Unit", "()")
    | :? bool                   -> Value("Boolean", "()")
    | :? System.Byte as v       -> Value ("Byte", sprintf "%duy" v)
    | :? System.Int32 as v      -> Value ("Int"  , string<int> v)
    | :? System.Int64 as v      -> Value ("Int64", string<int64> v)
    | :? System.Double as v     -> Value ("Float", string<float> v)
    | :? System.String as v     -> Value ("String", v)
    | :? System.DateTime as b   -> Value ("DateTime", sprintf "(%i, %i, %i, %i, %i, %i, %i)" b.Year b.Month b.Day b.Hour b.Minute b.Second b.Millisecond)
    | IsPrimitive(n)           -> Value("Primitive", n.ToString())
    | IsNullable(n)             -> print level n
    // | :? NavContext as n ->
    //     match n.Value with
    //     | null -> Html.Text("<null>")
    //     | HtmlObject(h) -> h.ToHtml
    //     | :? Html.Tag as t -> t
    //     | IsPrimitive(n) -> Html.Text(n.ToString())
    //     | GenericList(Primitive(_), _, list) -> printList (level - 1) list
    //     | GenericList(_, getters, list) -> printGenericNavList (level - 1)  getters list n
    //     | Object(members, _, _, _, _) -> printNavObject (level - 1) members n
    //     | o -> print level o
    // | GenericList(_) when level < 1 -> Html.Text("List...")
    // | :? System.Collections.IEnumerable when level < 1 -> Html.Text("List...")
    // | Object(_) when level < 1 -> Html.Text("Object...")
    // | _ when level < 1 -> Html.Text("...")
    // | GenericList(Primitive(_), _, list) -> printList (level - 1) list
    // | GenericList(_, getters, list) -> printGenericList (level - 1)  getters list
    // | :? System.Collections.IEnumerable as s -> printList (level - 1) s
    // | Object(members, _, _, _, _) -> printObject (level - 1) members o
    | _ -> Value("Error", "Dunno know how!")


"Hello" |> print 3
