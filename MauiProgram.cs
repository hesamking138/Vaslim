using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Handlers.Items;


#if WINDOWS
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility;
#endif

namespace Vaslim
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if WINDOWS
            CollectionViewHandler.Mapper.AppendToMapping("HideScrollBars", (handler, view) =>
            {
                handler.PlatformView.Loaded += (sender, args) =>
                {
                    var scrollViewer = FindDescendant<ScrollViewer>(handler.PlatformView);

                    if (scrollViewer != null)
                    {
                        scrollViewer.VerticalScrollBarVisibility = WinScrollBarVisibility.Hidden;
                        scrollViewer.HorizontalScrollBarVisibility = WinScrollBarVisibility.Hidden;
                    }
                };
            });
#endif

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

#if WINDOWS
        private static T? FindDescendant<T>(DependencyObject parent)
            where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T result)
                    return result;

                var descendant = FindDescendant<T>(child);

                if (descendant != null)
                    return descendant;
            }

            return null;
        }
#endif
    }
}