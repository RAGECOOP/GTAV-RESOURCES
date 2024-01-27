using GTA;
using GTA.UI;

namespace RageCoop.Resources.Examples.Client
{
    public class ExampleScript : ClientScript
    {
        public ExampleScript()
        {
            KeyDown += ExampleScript_KeyDown;
        }

        protected override void OnStart()
        {
            base.OnStart();
            Logger.Debug($"{nameof(ExampleScript)} started");
            Notification.Show("Press Y to say hello");
        }

        private void ExampleScript_KeyDown(KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Y)
                {
                    API.SendChatMessage($"hello from {CurrentResource.Name}");
                }
            }
            catch (Exception ex)
            {
                GTA.Console.PrintError(ex.ToString());
            }
        }
    }
}