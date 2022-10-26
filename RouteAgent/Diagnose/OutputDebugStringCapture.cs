using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.MemoryMappedFiles;
using System.IO;
using RouteAgent.Common;
using System.Diagnostics;
using CSBase.Diagnose;

namespace RouteAgent
{
    class OutputDebugStringCapture
    {
        protected static OutputDebugStringCapture m_captureIns = null;

        MemoryMappedFile memoryMappedFile = null;
        EventWaitHandle bufferReadyEvent = null;
        EventWaitHandle dataReadyEvent = null;

        protected OutputDebugStringCapture()
        {

        }

        public static OutputDebugStringCapture Instance()
        {
            if(m_captureIns==null)
            {
                m_captureIns = new OutputDebugStringCapture();
            }
            return m_captureIns;
        }

        public void Init()
        {
            try
            {
                memoryMappedFile = MemoryMappedFile.CreateNew("DBWIN_BUFFER", 4096L);

                // We try to create the events. If the events exist, we
                // will report an error and abort.
                bool created;
                bufferReadyEvent = new EventWaitHandle(
                    false,
                    EventResetMode.AutoReset,
                    "DBWIN_BUFFER_READY",
                    out created);
                if (!created)
                {
                    CSLogger.OutputLog(LogLevel.Error, "OutputDebugStringCapture The DBWIN_BUFFER_READY event exists.");
                    return;
                }

                dataReadyEvent = new EventWaitHandle(
                    false,
                    EventResetMode.AutoReset,
                    "DBWIN_DATA_READY",
                    out created);
                if (!created)
                {
                    CSLogger.OutputLog(LogLevel.Error, "OutputDebugStringCapture The DBWIN_DATA_READY event exists.");
                    return;
                }

                Thread readLogThread = new Thread(new ThreadStart(OutputDebugStringCaptureThread));
                readLogThread.Start();


            }
            catch(Exception ex)
            {
                CSLogger.OutputLog(LogLevel.Error, "OutputDebugStringCapture exception", null, ex);
                return;
            }
        }

        private void OutputDebugStringCaptureThread()
        {
            try
            {
                CSLogger.OutputLog(LogLevel.Info, "OutputDebugStringCaptureThread running.");

                //get current processid
                Process processes = Process.GetCurrentProcess();
                int nCurProcID = processes.Id;

                bufferReadyEvent.Set();
                while (dataReadyEvent.WaitOne())
                {
                    using (var stream = memoryMappedFile.CreateViewStream())
                    {
                        using (var reader = new BinaryReader(stream, Encoding.Default))
                        {
                            var processId = reader.ReadUInt32();
                            if (processId == nCurProcID)
                            {
                                // Because I specified the Encoding.Default object as the
                                // encoding for the BinaryReader object, the characters
                                // will be read out of memory as 8-bit octets and converted
                                // to Unicode characters by .NET.
                                var chars = reader.ReadChars(4092);

                                // The debug message will be null-terminated in memory,
                                // so I am looking for the null character in the character
                                // array to determine the bounds of the message to output.
                                int index = Array.IndexOf(chars, '\0');
                                var message = new string(chars, 0, index);

                                CSLogger.OutputLog(LogLevel.Info, message);
                            }
                        }
                    }

                    // The message has been processed, so trigger the
                    // DBWIN_BUFFER_READY event in order to receive the next message
                    // from the process being debugged.
                    bufferReadyEvent.Set();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                Console.ReadLine();
            }
        }
    }
}
