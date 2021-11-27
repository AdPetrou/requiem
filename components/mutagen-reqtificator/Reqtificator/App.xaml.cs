﻿using System;
using System.Windows;
using System.Windows.Threading;
using Reqtificator.Events;
using Reqtificator.Gui;
using Serilog;

namespace Reqtificator
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            MainWindow? window = null;
            try
            {
                var logContext = new ReqtificatorLogContext(LogUtils.DefaultLogFileName);
                Log.Information("Starting the Reqtificator");

                var eventQueue = new InternalEvents();
                var dispatcher = Dispatcher.CurrentDispatcher;

                Log.Debug("Starting Reqtificator Gui");
                MainWindowViewModel mainWindowViewModel = new(eventQueue);
                window = new() { DataContext = mainWindowViewModel };

                eventQueue.StateChanged += (_, state) =>
                {
                    Log.Information("Reqtificator state: " + state.Readable);
                    if (state is StoppedState stoppedState) { OnUiThread(dispatcher, () => HandlePatchingResult(window, stoppedState)); }
                };

                window.Show();

                var backend = new Backend(eventQueue, logContext);
                Log.Debug("Gui Started");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
                HandlePatchingResult(window, ReqtificatorState.Stopped(ReqtificatorFailure.CausedBy(ex)));
                window?.Close();
                throw;
            }
        }

        private static void OnUiThread(Dispatcher d, Action a)
        {
            d.Invoke(a);
        }

        private static void HandlePatchingResult(MainWindow? window, StoppedState state)
        {
            var outcome = state.Outcome;

            Log.Information("Patching Result: " + outcome.Status.ToString());
            try
            {
                Log.Debug("Opening message dialog");
                PatchStatusViewModel patchingFinishedViewModel = new(outcome);
                PatchStatusWindow pfWindow = new() { DataContext = patchingFinishedViewModel };
                patchingFinishedViewModel.CloseRequested += () =>
                {
                    Log.Debug("Closing all windows");
                    pfWindow.Close();
                    window?.Close();
                };
                patchingFinishedViewModel.ReturnRequested += () =>
                {
                    Log.Debug("Returning to main window");
                    pfWindow.Close();
                };
                _ = pfWindow.ShowDialog();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
                throw;
            }
        }

        private void App_SessionEnding(object _, SessionEndingCancelEventArgs ea)
        {
            Log.CloseAndFlush();
        }
    }
}
