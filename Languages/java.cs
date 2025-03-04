using System;
using System.Diagnostics;
using System.IO;
using api.Models;
using Microsoft.AspNetCore.Mvc;

namespace api.Languages
{
    public class Java : ControllerBase
    {
        public IActionResult Run(string source, ProblemStruct problem)
        {
            string randomFileName = Path.GetRandomFileName();
            string currentDirectory = Directory.GetCurrentDirectory();
            string tempDirectory = Path.Combine(currentDirectory, "tmp");

            if (!Directory.Exists(tempDirectory))
            {
                Directory.CreateDirectory(tempDirectory);
            }

            string sourceFile = Path.Combine(tempDirectory, $"{randomFileName}.java");
            string classFile = Path.Combine(tempDirectory, $"{randomFileName}.class");

            try
            {
                System.IO.File.WriteAllText(sourceFile, source);

                // Compile the Java code
                Process compileProcess = new Process();
                compileProcess.StartInfo.FileName = "javac"; // Use 'javac' to compile Java code
                compileProcess.StartInfo.Arguments = sourceFile;
                compileProcess.StartInfo.RedirectStandardOutput = true;
                compileProcess.StartInfo.RedirectStandardError = true;
                compileProcess.Start();
                compileProcess.WaitForExit();

                if (compileProcess.ExitCode != 0)
                {
                    string errorOutput = compileProcess.StandardError.ReadToEnd();
                    return BadRequest($"Compilation failed: {errorOutput}");
                }

                int c = 0;
                foreach (var testcase in problem.TestCases)
                {
                    c++;
                    Process runProcess = new Process();
                    runProcess.StartInfo.FileName = "java"; // Run with 'java' command to execute the .class file
                    runProcess.StartInfo.Arguments = $"-cp {tempDirectory} {randomFileName}"; // Class name without extension
                    runProcess.StartInfo.RedirectStandardInput = true;
                    runProcess.StartInfo.RedirectStandardOutput = true;
                    runProcess.StartInfo.RedirectStandardError = true;
                    runProcess.Start();

                    using (var writer = runProcess.StandardInput)
                    {
                        writer.WriteLine(testcase.input);
                    }

                    string output = runProcess.StandardOutput.ReadToEnd().Trim();
                    runProcess.WaitForExit();

                    if (output != testcase.expectedOutput.Trim())
                    {
                        return BadRequest($"Test case {c} failed. Expected: {testcase.expectedOutput}, Got: {output}");
                    }
                }

                return Ok("All test cases passed.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest($"File access error: {ex.Message}");
            }
            catch (IOException ex)
            {
                return BadRequest($"File system error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest($"An error occurred: {ex.Message}");
            }
            finally
            {
                if (System.IO.File.Exists(sourceFile))
                {
                    System.IO.File.Delete(sourceFile);
                }
                if (System.IO.File.Exists(classFile))
                {
                    System.IO.File.Delete(classFile);
                }
            }
        }
    }
}
