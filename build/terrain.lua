project "Terrain"
   generateMessages("../src/terrain/events")
   language  "C#"
   kind      "SharedLib"
   flags {"Unsafe"}
   files     { "../src/terrain/**.cs", "../src/terrain/**.glsl", "../src/terrain/**.event"  }
   links     { "System", "OpenTK", "Util", "Noise", "Graphics", "Events", "Network", "Physics" }
   location "terrain"
   vpaths { ["*"] = "../src/terrain" }