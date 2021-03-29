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

                foreach (ClrInfo clr in dataTarget.ClrVersions)
                {
                    using ClrRuntime runtime = clr.CreateRuntime();
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

                                var asstr = obj.AsString();
                                ulong objSize = obj.Size;
                                int generation = seg.GetGeneration(obj);

                                var keyWithGen = $"{asstr}_{objSize}_{generation}";
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
