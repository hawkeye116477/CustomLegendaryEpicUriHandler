namespace CustomLegendaryEpicUriHandler.Models
{
    public class LegendaryGameInfo
    {
        public class Rootobject
        {
            public Game Game { get; set; }
            public bool errorDisplayed { get; set; } = false;
        }
        
        public class Game
        {
            public string App_name { get; set; }
            public string Title { get; set; } = "";
            public string Version { get; set; } = "";
            public bool Is_dlc { get; set; } = false;
            public string External_activation { get; set; } = "";
        }
        
    }
}