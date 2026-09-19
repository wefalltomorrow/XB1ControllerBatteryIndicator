using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autofac;
using Caliburn.Micro;

namespace XB1ControllerBatteryIndicator
{
    //Caliburn.Micro.AutofacBootstrap (which provided AutofacBootstrapper<T>) hasn't been
    //updated since 2013 and has no build for modern .NET, so the container is wired up
    //directly against Caliburn.Micro's BootstrapperBase instead.
    internal class Bootstrapper : BootstrapperBase
    {
        private IContainer _container;

        public Bootstrapper()
        {
            Initialize();
        }

        protected override void Configure()
        {
            var builder = new ContainerBuilder();

            builder.RegisterInstance<IWindowManager>(new WindowManager());
            builder.RegisterType<SystemTrayViewModel>();

            _container = builder.Build();
        }

        protected override object GetInstance(Type service, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                if (_container.IsRegistered(service))
                    return _container.Resolve(service);
            }
            else if (_container.IsRegisteredWithName(key, service))
            {
                return _container.ResolveNamed(key, service);
            }

            throw new Exception($"Could not locate any instances of contract {key ?? service.Name}.");
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(service);
            return ((IEnumerable)_container.Resolve(enumerableType)).Cast<object>();
        }

        protected override void BuildUp(object instance)
        {
        }

        protected override async void OnStartup(object sender, StartupEventArgs e)
        {
            base.OnStartup(sender, e);

            await DisplayRootViewForAsync<SystemTrayViewModel>();
        }
    }
}
