﻿using System;

class LittleEndDemo
{
    public static void Main()
    {
        Console.WriteLine(
            "This example of the BitConverter.IsLittleEndian field " +
            "generates \nthe following output when run on " +
            "x86-class computers.\n");
        Console.WriteLine("IsLittleEndian:  {0}",
            BitConverter.IsLittleEndian);
    }
}
