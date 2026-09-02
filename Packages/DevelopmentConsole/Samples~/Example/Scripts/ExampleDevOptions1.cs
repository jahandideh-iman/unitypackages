using Arman.DevelopmentConsole;
using UnityEngine;


namespace Arman.Example.Development.DevelopmentConsole
{
    public class ExampleDevOptions1 : DevelopmentOptionsDefinition
    {

        [DevOption("Example DevOptions 1", "Option1")]
        public static void Option1()
        {
            Debug.Log("ExampleDevOptions1:Option1");
        }

        [DevOption("Example DevOptions 1", "Option2")]
        public static void LoadLevel(int value)
        {
            Debug.Log($"ExampleDevOptions1:Option1:{value}");
        }

    }
}