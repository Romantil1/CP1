using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CP1
{
    public partial class MainWindow : Window
    {
        // Для вкладки 1: Запуск внешних процессов
        private Process notepadProcess;

        // Для вкладки 2: Работа с потоками и Dispatcher
        private CancellationTokenSource cancellationTokenSource;

        // Для вкладки 3: Синхронизация через lock
        private int counter = 0;
        private readonly object lockObject = new object();

        // Для вкладки 4: Семафор
        private SemaphoreSlim semaphore = new SemaphoreSlim(3); // Ограничение в 3
        private List<string> sharedCollection = new List<string>();
        private int processedCount = 0;

        public MainWindow()
        {
            InitializeComponent();

            // Инициализация коллекции для семафора
            for (int i = 1; i <= 10; i++)
            {
                sharedCollection.Add($"Задача{i}");
            }
        }

        // Вкладка 1: Запуск Notepad
        private void StartNotepad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                notepadProcess = new Process();
                notepadProcess.StartInfo.FileName = "notepad.exe";
                notepadProcess.EnableRaisingEvents = true;
                notepadProcess.Exited += Notepad_Exited;
                notepadProcess.Start();

                StatusLabel1.Content = "Статус: Notepad запущен";
            }
            catch (Exception ex)
            {
                StatusLabel1.Content = $"Ошибка: {ex.Message}";
            }
        }

        private void Notepad_Exited(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                StatusLabel1.Content = "Статус: Notepad завершен";
            });
        }

        // Вкладка 2: Загрузка данных
        private async void StartLoad_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource = new CancellationTokenSource();
            StatusLabel2.Content = "Статус: Загрузка начата";

            await Task.Run(() => LoadData(cancellationTokenSource.Token));
        }

        private void StopLoad_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource?.Cancel();
            StatusLabel2.Content = "Статус: Загрузка остановлена";
        }

        private void LoadData(CancellationToken token)
        {
            for (int i = 0; i <= 100; i++)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                Thread.Sleep(50);

                Dispatcher.Invoke(() =>
                {
                    ProgressBar.Value = i;
                    StatusLabel2.Content = $"Статус: Загружено {i}%";
                });
            }

            Dispatcher.Invoke(() =>
            {
                StatusLabel2.Content = "Статус: Загрузка завершена";
            });
        }

        // Вкладка 3: Синхронизация lock
        private void StartThreadsLock_Click(object sender, RoutedEventArgs e)
        {
            counter = 0;
            ResultLabelLock.Content = "Результат: 0";

            Thread thread1 = new Thread(IncrementCounter);
            Thread thread2 = new Thread(IncrementCounter);

            thread1.Start();
            thread2.Start();

            thread1.Join();
            thread2.Join();

            ResultLabelLock.Content = $"Результат: {counter}";
        }

        private void IncrementCounter()
        {
            for (int i = 0; i < 100000; i++)
            {
                lock (lockObject)
                {
                    counter++;
                }
            }
        }

        // Вкладка 4: Семафор
        private async void StartThreadsSemaphore_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Clear();
            processedCount = 0;
            List<Task> tasks = new List<Task>();

            for (int i = 0; i < 10; i++)
            {
                int threadId = i;
                tasks.Add(Task.Run(() => ProcessItem(threadId)));
            }

            await Task.WhenAll(tasks);
            AppendLog("Все задачи обработаны.");
        }

        private async void ProcessItem(int threadId)
        {
            await semaphore.WaitAsync();
            try
            {
                if (processedCount < sharedCollection.Count)
                {
                    string item = sharedCollection[processedCount];
                    processedCount++;
                    AppendLog($"Поток {threadId} обрабатывает {item}...");
                    await Task.Delay(1000);
                    AppendLog($"Поток {threadId} завершил {item}.");
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private void AppendLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText($"{DateTime.Now}: {message}\n");
            });
        }
    }
}