using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using Graphics;
using UI;
using Util;

namespace UI
{
   public static partial class ImGui
   {
      public static void inputText(String label, ref String buffer)
      {
         Window win = currentWindow;
         if (win.skipItems)
            return;

         Vector2 labelSize = style.textSize(label) + ImGui.style.framePadding * 2.0f;

         win.canvas.addText(win.cursorPosition, style.colors[(int)ElementColor.Text], label);
         win.addItem(labelSize);
      }
   }
}