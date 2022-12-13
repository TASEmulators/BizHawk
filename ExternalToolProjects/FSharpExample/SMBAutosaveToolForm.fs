namespace Net.MyStuff.SMBAutosaveTool

open System
open System.Drawing
open System.Windows.Forms

open BizHawk.Client.Common
open BizHawk.Client.EmuHawk
open BizHawk.Emulation.Common

[<ExternalTool("SMB Autosave", LoadAssemblyFiles = [| "FSharp.Core.dll" |])>]
[<ExternalToolApplicability.RomList(
   VSystemID.Raw.NES,
   "EA343F4E445A9050D4B4FBAC2C77D0693B1D0922", // U
   "AB30029EFEC6CCFC5D65DFDA7FBC6E6489A80805")>] // E
type SMBAutosaveToolForm() as self =
   inherit ToolFormBase()

   let _lblLevel = new Label(AutoSize = true)

   let mutable _prevLevel = "1-1"

   let mutable _prevSlot = Nullable<int>()

   do
      base.ClientSize <- Size(480, 320)
      base.SuspendLayout()
      base.Controls.Add(_lblLevel)
      base.ResumeLayout(performLayout = false)
      base.PerformLayout()

   member val APIs : ApiContainer = null with get, set

   override this.WindowTitleStatic = "SMB Autosave"

   member private this.ReadLevel() =
      let bytes = self.APIs.Memory.ReadByteRange(0x075CL, 9)
      match bytes[8] with
      | 0uy | 0xFFuy -> _prevLevel // in the main menu
      | _ -> $"{bytes[3] + 1uy}-{bytes[0] + 1uy}"

   override this.Restart() =
      _prevLevel <- "1-1" // ReadLevel returns this when in the main menu, need to reset it
      _lblLevel.Text <- $"You are in World {self.ReadLevel()}"
      self.APIs.EmuClient.StateLoaded.Add(fun _ -> _prevLevel <- self.ReadLevel()) // without this, loading a state would cause UpdateAfter to save a state because the level would be different

   override this.UpdateAfter() =
      let level = self.ReadLevel()
      if level <> _prevLevel then // the player has just gone to the next level
         let nextSlot = ((if _prevSlot.HasValue then _prevSlot.Value else 0) + 1) % 10
         self.APIs.SaveState.SaveSlot(nextSlot)
         self.Config.SaveSlot <- nextSlot
         (self.MainForm :?> MainForm).UpdateStatusSlots()
         let mutable text = $"You are in World {level}, load slot {nextSlot} to restart"
         if _prevSlot.HasValue then text <- $"{text} or {_prevSlot} to go back to {_prevLevel}"
         _lblLevel.Text <- text
         _prevSlot <- Nullable(nextSlot)
         _prevLevel <- level

   interface IExternalToolForm
