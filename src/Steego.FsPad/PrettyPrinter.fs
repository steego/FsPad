module FsPad.PrettyPrinter

type PrettyPrint =
    | List  of list<PrettyPrint>
    | Optn  of option<PrettyPrint>
    | Table of list<Field>
    | Value of string * string
    | RawHtml of string
    | MaxRecurse
and Field = {name : string; value : PrettyPrint}

let htmlEncode s = System.Net.WebUtility.HtmlEncode(s)

let render x =
    let rec render forceTabular td x =
        match x with        
        | RawHtml(html) -> html
        | MaxRecurse -> "..."
        | Value (ty, vl) -> 
            if not td then htmlEncode vl
            else  "<td>" + htmlEncode vl + "</td>"
            
        | Optn lst ->
            if not td then
                match lst with
                | Some x -> render false false x
                | None   -> render false false (Value ("", "-"))
            else
                match lst with
                | Some x -> "<td style=some>" + render false false (Optn lst) + "</td>"
                | None   -> "<td class=none>" + render false false (Optn lst) + "</td>"

        | Table fields ->
            
            seq {
                
                if forceTabular then

                    for f in fields do
                        yield render false true f.value
                else
                    if td then yield "<td>"
                    yield "<table>"                            
                    for f in fields do
                        yield "<tr>"
                        yield "<th>" + htmlEncode f.name + "</th>"
                        yield render false true f.value
                        yield "</tr>"
                    yield "</table>" 
                    if td then yield "</td>"
                } |> String.concat "  "
        | List lst -> 
            let fields =
                lst |> List.fold (fun (s:Set<string>) x -> 
                        match x with
                        | Table x ->
                            let c = List.map (fun {name = n} -> n) x
                            set c + s
                        | _ -> s
                ) Set.empty

            let body =
                    seq {
                        yield "<table>"
                        yield "<tr>"
                        for j in fields do
                            yield "<th>" + htmlEncode j + "</th>"
                        yield "</tr>"

                        for e in lst do
                            yield "<tr>"
                            yield render true true e
                            yield "</tr>"
                        yield "</table>" 
                        } |> String.concat "  "
            if not td then body
            else "<td>" + body + "</td>"

    render false false x
