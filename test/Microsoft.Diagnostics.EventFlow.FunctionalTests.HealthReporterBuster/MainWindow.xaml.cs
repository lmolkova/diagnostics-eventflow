﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Diagnostics.EventFlow.HealthReporters;

namespace Microsoft.Diagnostics.EventFlow.FunctionalTests.HealthReporterBuster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Timer timer;
        CsvHealthReporter reporter;
        volatile int hit = 0;
        DispatcherTimer hitReporter = new DispatcherTimer();
        ManualNewReportTrigger manualTrigger = new ManualNewReportTrigger();

        public MainWindow()
        {
            InitializeComponent();
            Application.Current.Exit += Current_Exit;
        }

        private void Current_Exit(object sender, ExitEventArgs e)
        {
            if (reporter != null)
            {
                reporter.Dispose();
            }
        }

        private void btnStart_Clicked(object sender, RoutedEventArgs e)
        {
            CsvHealthReporterConfiguration configuration = new CsvHealthReporterConfiguration()
            {
                LogFileFolder = tbLogFileFolder.Text.Trim(),
                LogFilePrefix = tbLogFilePrefix.Text.Trim(),
                MinReportLevel = tbMinReportLevel.Text.Trim()
            };

            reporter = new CustomHealthReporter(configuration, this.manualTrigger);
            reporter.Activate();
            int intervalInMs;
            if (!int.TryParse(tbMessageInterval.Text, out intervalInMs))
            {
                intervalInMs = 500;
            }

            timer = new Timer(state =>
            {
                (state as CsvHealthReporter)?.ReportHealthy(DateTime.Now.ToString(CultureInfo.CurrentCulture.DateTimeFormat.SortableDateTimePattern), "HealthReporterBuster");
                hit++;
            }, reporter, 0, intervalInMs);

            hitReporter.Interval = TimeSpan.FromMilliseconds(500);
            hitReporter.Tick += HitReporter_Tick;
            hitReporter.IsEnabled = true;

            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;
            btnSwitch.IsEnabled = true;
        }

        private void HitReporter_Tick(object sender, EventArgs e)
        {
            tbHit.Text = hit.ToString();
        }

        private void btnSwitch_Clicked(object sender, RoutedEventArgs e)
        {
            this.manualTrigger?.TriggerChange();
        }

        private void btnStop_Clicked(object sender, RoutedEventArgs e)
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }

            if (reporter != null)
            {
                reporter.Dispose();
                reporter = null;
            }

            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            btnSwitch.IsEnabled = false;
        }

        private void btnCrashMe_Clicked(object sender, RoutedEventArgs e)
        {
            btnCrashMe.IsEnabled = false;
            reporter.ReportProblem("I crashed myself! Yeah~~");
            throw new Exception("Crash me button is clicked!");
        }
    }
}
