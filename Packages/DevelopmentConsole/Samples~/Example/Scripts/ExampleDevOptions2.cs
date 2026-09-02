using Arman.DevelopmentConsole;
using UnityEngine;


namespace Arman.Example.Development.DevelopmentConsole
{
    public class ExampleDevOptions2 : DevelopmentOptionsDefinition
    {

        [DevOption("Example DevOptions 2", "Option3")]
        public static void Option1()
        {
            Debug.Log("ExampleDevOptions2:Option3");
        }


    }
}