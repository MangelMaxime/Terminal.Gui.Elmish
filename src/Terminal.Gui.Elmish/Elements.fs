﻿namespace Terminal.Gui.Elmish

open System.Reflection
open System.Collections
open System




[<AutoOpen>]
module StyleHelpers =
    
    open Terminal.Gui

    type Position =
        | AbsPos of int
        | PercentPos of float
        | Center

    type Dimension =
        | Fill
        | FillMargin of int
        | AbsDim of int
        | PercentDim of float

    type Style =
        | Pos of x:Position * y:Position
        | Dim of width:Dimension * height:Dimension

    type Prop<'TValue> =
        | Styles of Style list
        | Value of 'TValue
        | Text of string
        | Title of string
        | OnChanged of ('TValue -> unit)
        | OnClicked of (unit -> unit)
        | Items of ('TValue * string) list
        | Frame of Rect

    
    let private convDim (dim:Dimension) =
        match dim with
        | Fill -> Dim.Fill()
        | FillMargin m -> Dim.Fill(m)
        | AbsDim i -> Dim.Sized(i)
        | PercentDim p -> Dim.Percent(p |> float32)

    let private convPos (dim:Position) =
        match dim with
        | Position.AbsPos i -> Pos.At(i)
        | Position.PercentPos p -> Pos.Percent(p |> float32)
        | Position.Center -> Pos.Center()
    
    let addStyleToView (view:View) (style:Style) =
        match style with
        | Pos (x,y) ->
            view.X <- x |> convPos
            view.Y <- y |> convPos
        | Dim (width,height) ->
            view.Width <- width |> convDim
            view.Height <- height |> convDim
    
    let addStyles (styles:Style list) (view:View)=
        styles
        |> List.iter (fun si ->
            si |> addStyleToView view                    
        )

    let tryGetStylesFromProps (props:Prop<'TValue> list) =
        props
        |> List.tryFind (fun i -> match i with | Styles _ -> true | _ -> false)

    let inline addPossibleStylesFromProps (props:Prop<'TValue> list) (view:'T when 'T :> View) =
        let styles = tryGetStylesFromProps props
        match styles with
        | None ->
            view
        | Some (Styles styles) ->
            view |> addStyles styles
            view
        | Some _ ->
            view

    
    let getTitleFromProps (props:Prop<'TValue> list) = 
        props
        |> List.tryFind (fun i -> match i with | Title _ -> true | _ -> false)
        |> Option.map (fun i -> match i with | Title t -> t | _ -> "")
        |> Option.defaultValue ""

    let getTextFromProps (props:Prop<'TValue> list) = 
        props
        |> List.tryFind (fun i -> match i with | Text _ -> true | _ -> false)
        |> Option.map (fun i -> match i with | Text t -> t | _ -> "")
        |> Option.defaultValue ""

    let tryGetValueFromProps (props:Prop<'TValue> list) = 
        props
        |> List.tryFind (fun i -> match i with | Value _ -> true | _ -> false)
        |> Option.map (fun i -> match i with | Value t -> t | _ -> failwith "What?No!Never should this happen!")

    let tryGetFrameFromProps (props:Prop<'TValue> list) = 
        props
        |> List.tryFind (fun i -> match i with | Frame _ -> true | _ -> false)
        |> Option.map (fun i -> match i with | Frame t -> t | _ -> failwith "What?No!Never should this happen!")

    let tryGetOnChangedFromProps (props:Prop<'TValue> list) = 
        props
        |> List.tryFind (fun i -> match i with | OnChanged _ -> true | _ -> false)
        |> Option.map (fun i -> match i with | OnChanged t -> t | _ -> failwith "What?No!Never should this happen!")

    let tryGetOnClickedFromProps (props:Prop<'TValue> list) = 
        props
        |> List.tryFind (fun i -> match i with | OnClicked _ -> true | _ -> false)
        |> Option.map (fun i -> match i with | OnClicked t -> t | _ -> failwith "What?No!Never should this happen!")

    let getItemsFromProps (props:Prop<'TValue> list) = 
        props
        |> List.tryFind (fun i -> match i with | Items _ -> true | _ -> false)
        |> Option.map (fun i -> match i with | Items t -> t | _ -> failwith "What?No!Never should this happen!")
        |> Option.defaultValue []
        
       

    



[<AutoOpen>]
module Elements =

    open Terminal.Gui
    open NStack

    let ustr (x:string) = ustring.Make(x)


    let page (subViews:View list) =
        let top = Toplevel.Create()
        subViews |> List.iter (fun v -> top.Add(v))
        let state = Application.Begin(top)  
        state
       


    let window (props:Prop<'TValue> list) (subViews:View list) =        
        let title = getTitleFromProps props
        let window = Window(title |> ustr)
        subViews |> List.iter (fun v -> window.Add(v))        
        window
        |> addPossibleStylesFromProps props

    let button (props:Prop<'TValue> list) = 
        let text = getTextFromProps props
        let b = Button(text |> ustr)
        let clicked = tryGetOnClickedFromProps props
        match clicked with
        | Some clicked ->
            b.Clicked <- Action((fun () -> clicked() ))
        | None ->
            ()
        b
        |> addPossibleStylesFromProps props

    let label (props:Prop<'TValue> list) =   
        let text = getTextFromProps props
        let l = Label(text |> ustr)
        l
        |> addPossibleStylesFromProps props

    let textField (props:Prop<string> list) =        
        let value = 
            tryGetValueFromProps props
            |> Option.defaultValue ""
        
        let t = TextField(value |> ustr)
        // Meh reflection to set the "used" flag to true, because
        // with the complete redraw approache, the text field will
        // be deleted if you try to enter something
        let tfields = t.GetType().GetFields(BindingFlags.Instance ||| BindingFlags.NonPublic)
        let tused = tfields |> Array.tryFind (fun e -> e.Name = "used")
        match tused with
        | Some tp ->
            tp.SetValue(t,true)
        | None -> ()

        let changed = tryGetOnChangedFromProps props
        match changed with
        | Some changed ->
            t.Changed.AddHandler(fun o _ -> changed (((o:?>TextField).Text).ToString()))        
        | None -> ()
        t
        |> addPossibleStylesFromProps props

    let textView (props:Prop<'TValue> list) =
        let text = getTextFromProps props
        let t = TextView()
        t.Text <- (text|> ustr)
        t
        |> addPossibleStylesFromProps props

   

    let frameView (props:Prop<'TValue> list) (subViews:View list) =
        let text = getTextFromProps props
        let f = FrameView(text |> ustr)
        subViews |> List.iter (fun v -> f.Add(v))
        f
        |> addPossibleStylesFromProps props

    let hexView (props:Prop<'TValue> list) stream =
        HexView(stream)
        |> addPossibleStylesFromProps props

    let inline listView (props:Prop<'TValue> list) = 
        let items = getItemsFromProps props
        let displayValues = items |> List.map (snd) |> List.toArray :> IList
        let value = tryGetValueFromProps props
        let selectedIdx = 
            value
            |> Option.bind (fun value ->
                items |> List.tryFindIndex (fun (v,_) -> v = value) 
            )
            
        let lv = 
            ListView(displayValues)
            |> addPossibleStylesFromProps props
        let addSelectedChanged (lv:ListView) =
            let onChange =
                tryGetOnChangedFromProps props
            match onChange with
            | Some onChange ->
                let action = Action((fun () -> 
                    let (value,disp) = items.[lv.SelectedItem]
                    onChange (value)
                ))
                lv.add_SelectedChanged(action)
                lv
            | None ->
                lv
        
        match selectedIdx with
        | None ->
            lv
            |> addSelectedChanged
            |> addPossibleStylesFromProps props
        | Some idx ->
            lv.SelectedItem <- idx
            lv
            |> addSelectedChanged
            |> addPossibleStylesFromProps props
            
            

    let menuItem title help action = 
        MenuItem(title |> ustr,help ,(fun () -> action () ))

    let menuBarItem text (items:MenuItem list) = 
        MenuBarItem(text |> ustr,items |> List.toArray)

    let menuBar (items:MenuBarItem list) = 
        MenuBar (items |> List.toArray)



    let progressBar (props:Prop<float> list) = 
        let value = 
            tryGetValueFromProps props
            |> Option.defaultValue 0.0

        let pb = ProgressBar(Fraction = (value |> float32))        
        pb
        |> addPossibleStylesFromProps props

    let checkBox (props:Prop<bool> list) = 
        let isChecked = 
            tryGetValueFromProps props
            |> Option.defaultValue false

        let text = getTextFromProps props
        let cb = CheckBox(text |> ustr,isChecked)
        let onChanged = tryGetOnChangedFromProps props
        match onChanged with
        | Some onChanged ->
            cb.Toggled.AddHandler((fun o e -> (o:?>CheckBox).Checked |> onChanged))
        | None ->
            ()
        cb
        |> addPossibleStylesFromProps props
    

    let setCursorRadioGroup (x:int) (rg:RadioGroup) =
        let tfields = rg.GetType().GetFields(BindingFlags.Instance ||| BindingFlags.NonPublic)
        let tused = tfields |> Array.tryFind (fun e -> e.Name = "cursor")
        match tused with
        | Some tp ->
            tp.SetValue(rg,x)
            rg
        | None -> rg

    let inline radioGroup (props:Prop<'TValue> list) =
        let items = getItemsFromProps props        
        let value = tryGetValueFromProps props
        let displayValues = items |> List.map (snd) |> List.toArray
        let idxItem = 
            value
            |> Option.bind (fun value ->
                items |> List.tryFindIndex (fun (v,_) -> v = value)
            )

        let addSelectedChanged (rg:RadioGroup) =
            let onChange =
                tryGetOnChangedFromProps props
            match onChange with
            | Some onChange ->
                let action = Action<int>((fun idx -> 
                    let (value,disp) = items.[idx]
                    onChange (value)
                ))
                rg.SelectionChanged <- action
                rg
            | None ->
                rg

        match idxItem with
        | None ->
            RadioGroup(displayValues)
            |> addSelectedChanged
            |> addPossibleStylesFromProps props
        | Some idx ->
            RadioGroup(displayValues,idx)
            |> setCursorRadioGroup idx
            |> addSelectedChanged
            |> addPossibleStylesFromProps props
            
        

    
    let scrollView (props:Prop<'TValue> list) (subViews:View list) =
        let frame = tryGetFrameFromProps props
        match frame with
        | None ->
            failwith "Scrollview need a Frame Prop"
        | Some frame ->
            let sv = ScrollView(frame)
            subViews |> List.iter (fun i -> sv.Add(i))
            sv
            |> addPossibleStylesFromProps props

    


    

    