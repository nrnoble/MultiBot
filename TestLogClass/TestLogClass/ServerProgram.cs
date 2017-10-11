﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WindowsInput;
using WindowsInput.Native;
using EnvControllers;
using SimpleTCP;
using System.IO;
using Enigma.D3.ApplicationModel;
using Enigma.D3.Assets;
using Enigma.D3.MemoryModel;
using Enigma.D3.MemoryModel.Core;
using Enigma.D3;
using Enigma.D3.MemoryModel.Controls;
using static Enigma.D3.MemoryModel.Core.UXHelper;
using SlimDX.DirectInput;
using System.Runtime.InteropServices;

namespace MultibotPrograms
{
    class ServerProgram
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Server Program Started: making RosbotController");
            string pathToFile = @"C:\Users\GuilhermeMarques\Documents\RoS-BoT\Logs\logs.txt";
            RosController rosCon = new RosController(pathToFile);
            Console.WriteLine("Starting server");
            ServerController server = new ServerController();
            server.port = 8910;
            server.pathToLogFile = pathToFile;
            server.Start();
            server.StartModules();
            Console.WriteLine("All modules started: reading game states");
            server.sendMessage("Server started modules");
            while (true) {
                server.gameState.UpdateGameState();
                var newLogLines = server.rosController.rosLog.NewLines;
                
                if (LogFile.LookForString(newLogLines, "Vendor Loop Done") & !server.rosController.vendorLoopDone) {
                    //pause after vendor loop done
                    server.rosController.vendorLoopDone = true;
                    server.rosController.enteredRift = false;
                    server.sendMessage("Vendor Loop Done");
                    Console.WriteLine("Vendor Loop Done Detected");
                    if (!server.rosController.otherVendorLoopDone)
                    {
                        server.rosController.Pause();
                    }
                    Thread.Sleep(100);
                }

                if (LogFile.LookForString(newLogLines, "Next rift in different") & !server.gameState.inMenu)
                {   
                    //failure detected
                    server.sendMessage("Go to menu");
                    server.GoToMenu();
                    Console.WriteLine("Next rift in different game detected: Go to menu and send Go to menu");
                    Thread.Sleep(500);
                }

                if (server.gameState.acceptgrUiVisible) {
                    // grift accept request: always click cancel
                    server.rosController.enteredRift = false;
                    var xCoord = server.gameState.acceptgrUiControl.uirect.TranslateToClientRect(server.gameState.clientWidth, server.gameState.clientHeight).Left +
                        (server.gameState.acceptgrUiControl.uirect.TranslateToClientRect(server.gameState.clientWidth, server.gameState.clientHeight).Width / 2);
                    var yCoord = server.gameState.acceptgrUiControl.uirect.TranslateToClientRect(server.gameState.clientWidth, server.gameState.clientHeight).Top +
                        (server.gameState.acceptgrUiControl.uirect.TranslateToClientRect(server.gameState.clientWidth, server.gameState.clientHeight).Height * 1.5);
                    RosController.SetCursorPos((int)xCoord, (int)yCoord);
                    server.rosController.inputSimulator.Mouse.LeftButtonClick();                    
                    Console.WriteLine("Accept Rift Dialog Detected: Click Cancel");
                    Thread.Sleep(1500);                   
                }

                if (server.gameState.cancelgriftUiVisible)
                {
                    //click cancel ok
                    server.rosController.Pause();
                    var xCoord = server.gameState.confirmationUiControl.uirect.TranslateToClientRect(server.gameState.clientWidth, server.gameState.clientHeight).Left +
                        (server.gameState.confirmationUiControl.uirect.TranslateToClientRect(server.gameState.clientWidth, server.gameState.clientHeight).Width / 2);
                    var yCoord = server.gameState.confirmationUiControl.uirect.TranslateToClientRect(server.gameState.clientWidth, server.gameState.clientHeight).Top +
                        (server.gameState.confirmationUiControl.uirect.TranslateToClientRect(server.gameState.clientWidth, server.gameState.clientHeight).Height / 2);
                    RosController.SetCursorPos((int)xCoord, (int)yCoord);
                    server.rosController.inputSimulator.Mouse.LeftButtonClick();
                    Console.WriteLine("Rift Cancelled Dialog Detected: Click Cancel");
                    Thread.Sleep(1500);
                }

                if (server.gameState.firstlevelRift & !server.rosController.enteredRift)
                {
                    //unpause after entering rift and reinit variables
                    Thread.Sleep(1500);
                    server.rosController.enteredRift = true;
                    server.rosController.Unpause();
                    server.rosController.InitVariables();
                    Thread.Sleep(500);
                    Console.WriteLine("First Floor Rift Detected: Unpausing and Reiniting variables");
                }

                if (server.gameState.haveUrshiActor)
                {   
                    //set Urshi state
                    server.rosController.didUrshi = true;
                    //send have urushi to other if didnt yet
                    if (!server.rosController.sentUrshi)
                    {
                        server.sendMessage("Teleport");
                        server.rosController.sentUrshi = true;
                        Console.WriteLine("Sent Teleport for Urshi");
                    }
                }
            }
        }

    }
}