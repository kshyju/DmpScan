namespace DmpScanConsole
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Diagnostics.Runtime;

    internal sealed class Scanner
    {
        public static void Analyze(string filePath)
        {
            Dictionary<string, int> stringDupesDict = new Dictionary<string, int>();
            using (DataTarget dataTarget = DataTarget.LoadDump(filePath))
            {
                Console.WriteLine("Starting analysis...");

                foreach (ClrInfo clrInfo in dataTarget.ClrVersions)
                {
                    //PrintGen(clrInfo);
                    PrintDuplicateStrings(clrInfo);
                }
            }


        }

        private static void PrintGen(ClrInfo clr)
        {
            Dictionary<string, int> stringDupesDict = new Dictionary<string, int>();

            ClrRuntime runtime = clr.CreateRuntime();
            if (!runtime.Heap.CanWalkHeap)
            {
                Console.WriteLine("Cannot walk the heap!");
            }
            else
            {
                foreach (ClrSegment segment in runtime.Heap.Segments)
                {
                    string type;
                    if (segment.IsEphemeralSegment)
                        type = "Ephemeral";
                    else if (segment.IsLargeObjectSegment)
                        type = "Large";
                    else
                        type = "Gen2";

                    Console.WriteLine($"{type} object range: {segment.ObjectRange} committed memory range: {segment.CommittedMemory} reserved memory range: {segment.ReservedMemory}");
                }
            
            }
            Console.WriteLine("Done");
        }

        private static void PrintDuplicateStrings(ClrInfo clr)
        {
            Dictionary<string, int> stringDupesDict = new Dictionary<string, int>();

            ClrRuntime runtime = clr.CreateRuntime();
            if (!runtime.Heap.CanWalkHeap)
            {
                Console.WriteLine("Cannot walk the heap!");
            }
            else
            {
                foreach (ClrSegment seg in runtime.Heap.Segments)
                {
                    foreach (ClrObject obj in seg.EnumerateObjects())
                    {
                        // If heap corruption, continue past this object.
                        if (!obj.IsValid)
                        {
                            continue;
                        }

                        if (obj.Type.Name != "System.String")
                        {
                            continue;
                        }

                        string type;
                        if (seg.IsEphemeralSegment)
                            type = "Ephemeral";
                        else if (seg.IsLargeObjectSegment)
                            type = "Large";
                        else
                            type = "Gen2";

                        var asstr = obj.AsString();
                        ulong objSize = obj.Size;
                        int generation = seg.GetGeneration(obj);

                        //Console.WriteLine($"{obj} {objSize} {generation}");
                        
                        var keyWithGen = $"{asstr}_{objSize}_{generation}_{type}";
                        if (stringDupesDict.TryGetValue(keyWithGen, out int val))
                        {
                            val = val + 1;
                            stringDupesDict[keyWithGen] = val;
                        }
                        else
                        {
                            stringDupesDict[keyWithGen] = 1;
                        }
                    }
                }
            }

            foreach (var d in stringDupesDict.OrderBy(a => a.Value))
            {
                Console.WriteLine($"Count:{d.Value},  String(value_size_gen):{d.Key}");
            }

            var totalStrings = stringDupesDict.Sum(a => a.Value);
            Console.WriteLine($"Total strings: {totalStrings}");
        }
    }
}
