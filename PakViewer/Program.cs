namespace PakViewer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Form1 form = new Form1();
            
            // get file address from args

            string fileAddress = "";
            if (Environment.GetCommandLineArgs().Length > 1)
            {
                fileAddress = Environment.GetCommandLineArgs()[1];
                if (!File.Exists(fileAddress))
                {
                    MessageBox.Show("File not found: " + fileAddress, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                form.OpenPakFile(fileAddress);
            }
            Application.Run(form);
        }
    }
}