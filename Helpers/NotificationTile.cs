using Microsoft.Toolkit.Uwp.Notifications;
using System;
using Windows.UI.Notifications;

namespace CampusNet
{
    class NotificationTile
    {
        private TileContent content;

        public NotificationTile(string username, string usage, string balance, string network)
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse();

            content = new TileContent()
            {
                Visual = new TileVisual()
                {
                    TileMedium = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = username
                                },
                                new AdaptiveText()
                                {
                                    Text = usage
                                },
                                new AdaptiveText()
                                {
                                    Text = balance
                                },
                                new AdaptiveText()
                                {
                                    Text = network
                                }
                            }
                        }
                    },

                    TileWide = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = resourceLoader.GetString("Account")+": "+username
                                },
                                new AdaptiveText()
                                {
                                    Text = resourceLoader.GetString("Usage")+": "+usage
                                },
                                new AdaptiveText()
                                {
                                    Text = resourceLoader.GetString("Balance")+ ": "+balance,
                                },
                                new AdaptiveText()
                                {
                                    Text = resourceLoader.GetString("Network")+ ": "+network,
                                }
                            }
                        }
                    },

                    TileLarge = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = resourceLoader.GetString("Account")+": "+username
                                },
                                new AdaptiveText()
                                {
                                    Text = resourceLoader.GetString("Usage")+": "+usage
                                },
                                new AdaptiveText()
                                {
                                    Text = resourceLoader.GetString("Balance")+ ": "+balance,
                                },
                                new AdaptiveText()
                                {
                                    Text = resourceLoader.GetString("Network")+ ": "+network,
                                }
                            }
                        }
                    },
                }
            };
        }

        public void Update()
        {
            var tile = new TileNotification(content.GetXml())
            {
                ExpirationTime = DateTimeOffset.Now.AddHours(3)
            };
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tile);
        }
    }
}
