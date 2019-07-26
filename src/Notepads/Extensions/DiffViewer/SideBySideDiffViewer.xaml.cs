﻿
namespace Notepads.Extensions.DiffViewer
{
    using Notepads.Commands;
    using Notepads.Services;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Windows.System;
    using Windows.UI;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Media;

    public sealed partial class SideBySideDiffViewer : UserControl, ISideBySideDiffViewer
    {
        private readonly RichTextBlockDiffRenderer _diffRenderer;
        private readonly ScrollViewerSynchronizer _scrollSynchronizer;
        private readonly IKeyboardCommandHandler<KeyRoutedEventArgs> _keyboardCommandHandler;

        public event EventHandler OnCloseEvent;

        public SideBySideDiffViewer()
        {
            InitializeComponent();
            _scrollSynchronizer = new ScrollViewerSynchronizer(new List<ScrollViewer> { LeftScroller, RightScroller });
            _diffRenderer = new RichTextBlockDiffRenderer();
            _keyboardCommandHandler = GetKeyboardCommandHandler();

            LeftBox.SelectionHighlightColor = Application.Current.Resources["SystemControlForegroundAccentBrush"] as SolidColorBrush;
            RightBox.SelectionHighlightColor = Application.Current.Resources["SystemControlForegroundAccentBrush"] as SolidColorBrush;

            ThemeSettingsService.OnAccentColorChanged += (sender, color) =>
            {
                LeftBox.SelectionHighlightColor =
                    Application.Current.Resources["SystemControlForegroundAccentBrush"] as SolidColorBrush;
                RightBox.SelectionHighlightColor =
                    Application.Current.Resources["SystemControlForegroundAccentBrush"] as SolidColorBrush;
            };

            LayoutRoot.KeyDown += OnKeyDown;
            KeyDown += OnKeyDown;
            LeftBox.KeyDown += OnKeyDown;
            RightBox.KeyDown += OnKeyDown;
        }

        private KeyboardCommandHandler GetKeyboardCommandHandler()
        {
            return new KeyboardCommandHandler(new List<IKeyboardCommand<KeyRoutedEventArgs>>
            {
                new KeyboardShortcut<KeyRoutedEventArgs>(false, false, false, VirtualKey.Escape, (args) =>
                {
                    OnCloseEvent?.Invoke(this, EventArgs.Empty);
                }),
                new KeyboardShortcut<KeyRoutedEventArgs>(false, true, false, VirtualKey.D, (args) =>
                {
                    OnCloseEvent?.Invoke(this, EventArgs.Empty);
                }),
            });
        }

        private void OnKeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs args)
        {
            _keyboardCommandHandler.Handle(args);
        }

        public void Focus()
        {
            RightBox.Focus(FocusState.Programmatic);
        }

        public void ClearCache()
        {
            LeftBox.TextHighlighters.Clear();
            LeftBox.Blocks.Clear();
            RightBox.TextHighlighters.Clear();
            RightBox.Blocks.Clear();
        }

        public void RenderDiff(string left, string right)
        {
            ClearCache();

            var foregroundBrush = (ThemeSettingsService.ThemeMode == ElementTheme.Dark)
                ? new SolidColorBrush(Colors.White)
                : new SolidColorBrush(Colors.Black);

            var diffContext = _diffRenderer.GenerateDiffViewData(left, right, foregroundBrush);
            var leftContext = diffContext.Item1;
            var rightContext = diffContext.Item2;
            var leftHighlighters = leftContext.GetTextHighlighters();
            var rightHighlighters = rightContext.GetTextHighlighters();

            Task.Factory.StartNew(async () =>
            {
                var leftCount = leftContext.Blocks.Count;
                var rightCount = rightContext.Blocks.Count;

                //var count = rightCount > leftCount ? rightCount : leftCount;

                //for (int i = 0; i < count; i++)
                //{
                //    if (i  < leftCount)
                //    {
                //        var j = i;
                //        await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                //        {
                //            Thread.Sleep(20);
                //            LeftBox.Blocks.Add(leftContext.Blocks[j]);
                //        });
                //    }
                //    if (i  < rightCount)
                //    {
                //        var j = i;
                //        await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                //        {
                //            Thread.Sleep(20);
                //            RightBox.Blocks.Add(rightContext.Blocks[j]);
                //        });
                //    }
                //}

                var leftStartIndex = 0;
                var rightStartIndex = 0;
                var threshold = 1;

                while (true)
                {
                    Thread.Sleep(1);
                    if (leftStartIndex < leftCount)
                    {
                        var end = leftStartIndex + threshold;
                        if (end >= leftCount) end = leftCount;
                        var start = leftStartIndex;

                        await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                        {
                            for (int x = start; x < end; x++)
                            {
                                LeftBox.Blocks.Add(leftContext.Blocks[x]);
                            }
                        });
                    }

                    if (rightStartIndex < rightCount)
                    {
                        var end = rightStartIndex + threshold;
                        if (end >= rightCount) end = rightCount;
                        var start = rightStartIndex;

                        await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                        {
                            for (int x = start; x < end; x++)
                            {
                                RightBox.Blocks.Add(rightContext.Blocks[x]);
                            }
                        });
                    }

                    leftStartIndex += threshold;
                    rightStartIndex += threshold;
                    threshold *= 5;

                    if (leftStartIndex >= leftCount && rightStartIndex >= rightCount)
                    {
                        break;
                    }
                }
            });

            Task.Factory.StartNew(async () =>
            {
                var leftCount = leftHighlighters.Count;
                var rightCount = rightHighlighters.Count;

                var leftStartIndex = 0;
                var rightStartIndex = 0;
                var threshold = 5;

                while (true)
                {
                    Thread.Sleep(10);
                    if (leftStartIndex < leftCount)
                    {
                        var end = leftStartIndex + threshold;
                        if (end >= leftCount) end = leftCount;
                        var start = leftStartIndex;

                        await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                        {
                            for (int x = start; x < end; x++)
                            {
                                LeftBox.TextHighlighters.Add(leftHighlighters[x]);
                            }
                        });
                    }

                    if (rightStartIndex < rightCount)
                    {
                        var end = rightStartIndex + threshold;
                        if (end >= rightCount) end = rightCount;
                        var start = rightStartIndex;

                        await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                        {
                            for (int x = start; x < end; x++)
                            {
                                RightBox.TextHighlighters.Add(rightHighlighters[x]);
                            }
                        });
                    }

                    leftStartIndex += threshold;
                    rightStartIndex += threshold;
                    threshold *= 5;

                    if (leftStartIndex >= leftCount && rightStartIndex >= rightCount)
                    {
                        break;
                    }
                }
            });

            //Task.Factory.StartNew(async () =>
            //{
            //    var leftCount = leftHighlighters.Count;
            //    var rightCount = rightHighlighters.Count;

            //    var count = rightCount > leftCount ? rightCount : leftCount;

            //    for (int i = 0; i < count; i++)
            //    {
            //        if (i < leftCount)
            //        {
            //            var j = i;
            //            await Dispatcher.RunAsync(CoreDispatcherPriority.Low,
            //                () => LeftBox.TextHighlighters.Add(leftHighlighters[j]));
            //        }
            //        if (i < rightCount)
            //        {
            //            var j = i;
            //            await Dispatcher.RunAsync(CoreDispatcherPriority.Low,
            //                () => RightBox.TextHighlighters.Add(rightHighlighters[j]));
            //        }
            //    }
            //});
        }
    }
}
