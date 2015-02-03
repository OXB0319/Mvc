// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultControllerFactory : IControllerFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IControllerActivator _controllerActivator;
        private static readonly Func<Type, ObjectFactory> CreateFactory =
            (t) => ActivatorUtilities.CreateFactory(t, Type.EmptyTypes);
        private static ConcurrentDictionary<Type, ObjectFactory> _controllerCreatorCache =
            new ConcurrentDictionary<Type, ObjectFactory>();

        public DefaultControllerFactory(IServiceProvider serviceProvider,
                                        IControllerActivator controllerActivator)
        {
            _serviceProvider = serviceProvider;
            _controllerActivator = controllerActivator;
        }

        public object CreateController(ActionContext actionContext)
        {
            var actionDescriptor = actionContext.ActionDescriptor as ControllerActionDescriptor;
            if (actionDescriptor == null)
            {
                throw new ArgumentException(
                    Resources.FormatActionDescriptorMustBeBasedOnControllerAction(
                        typeof(ControllerActionDescriptor)),
                    nameof(actionContext));
            }

            var controllerType = actionDescriptor.ControllerTypeInfo.AsType();

            var controllerFactory = _controllerCreatorCache.GetOrAdd(controllerType, CreateFactory);
            var controller = controllerFactory(_serviceProvider, null);

            _controllerActivator.Activate(controller, actionContext);

            return controller;
        }

        public void ReleaseController(object controller)
        {
            var disposableController = controller as IDisposable;

            if (disposableController != null)
            {
                disposableController.Dispose();
            }
        }
    }
}
