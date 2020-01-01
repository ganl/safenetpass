using System;
using System.Collections.Generic;
using System.Windows.Automation;
using CommandLine;

namespace safenetpass
{
    class Program
    {
        public class Options
        {
            [Option(
                Default = false,
                HelpText = "Prints all messages to standard output.")]
            public bool Verbose { get; set; }

            [Option('p', "password", Required = false, HelpText = "Set SafeNet key's password.")]
            public string Password { get; set; }

        }

        static void Main(string[] args)
        {
            Console.WriteLine("press q/Q to quit...");
            CommandLine.Parser.Default.ParseArguments<Options>(args)
              .WithParsed(RunOptions);
        }
        static void RunOptions(Options opts)
        {
            var passwd = "123456";
            //if (opts.Password.Length > 0)
            if (!string.IsNullOrEmpty(opts.Password))
            {
                passwd = opts.Password;
                Console.WriteLine($"Current Arguments: -p {opts.Password}");
                Console.WriteLine("USB key pass: {0}", passwd);
            }
            SatisfyEverySafeNetTokenPasswordRequest(passwd);
        }
        static void HandleParseError(IEnumerable<Error> errs)
        {
            //
        }

        static void SatisfyEverySafeNetTokenPasswordRequest(string password)
        {
            int count = 0;
            Automation.AddAutomationEventHandler(WindowPattern.WindowOpenedEvent, AutomationElement.RootElement, TreeScope.Children, (sender, e) =>
            {
                var element = sender as AutomationElement;
                if (element.Current.Name == "Token Logon" || element.Current.Name == "设备登录")
                {
                    WindowPattern pattern = (WindowPattern)element.GetCurrentPattern(WindowPattern.Pattern);
                    pattern.WaitForInputIdle(10000);
                    var edit = element.FindFirst(TreeScope.Descendants, new AndCondition(
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit),
                        new PropertyCondition(AutomationElement.NameProperty, "Token Password:")));

                    var pass = element.FindFirst(TreeScope.Descendants, new AndCondition(
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit),
                        new PropertyCondition(AutomationElement.NameProperty, "令牌密码:")));

                    var ok = element.FindFirst(TreeScope.Descendants, new AndCondition(
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                        new PropertyCondition(AutomationElement.NameProperty, "OK")));

                    var confirmBtn = element.FindFirst(TreeScope.Descendants, new AndCondition(
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                        new PropertyCondition(AutomationElement.NameProperty, "确定")));

                    if (edit != null && ok != null)
                    {
                        count++;
                        Login(password, count, edit, ok);
                    } else if (pass != null && confirmBtn != null) {
                        count++;
                        Login(password, count, pass, confirmBtn);
                    }
                    else
                    {
                        Console.WriteLine("SafeNet window detected but not with edit and button...");
                    }
                }
            });

            do
            {
                // press Q to quit...
                ConsoleKeyInfo k = Console.ReadKey(true);
                if (k.Key == ConsoleKey.Q)
                    break;
            }
            while (true);
            Automation.RemoveAllEventHandlers();
        }

        private static void Login(string password, int count, AutomationElement edit, AutomationElement ok)
        {
            ValuePattern vp = (ValuePattern)edit.GetCurrentPattern(ValuePattern.Pattern);
            vp.SetValue(password);
            Console.WriteLine("SafeNet window (count: " + count + " window(s)) detected. Setting password...");

            InvokePattern ip = (InvokePattern)ok.GetCurrentPattern(InvokePattern.Pattern);
            ip.Invoke();
        }
    }
}
