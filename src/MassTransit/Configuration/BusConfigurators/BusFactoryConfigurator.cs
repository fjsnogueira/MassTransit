﻿// Copyright 2007-2016 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.BusConfigurators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Builders;
    using GreenPipes;


    public abstract class BusFactoryConfigurator<TBuilder>
        where TBuilder : IBusBuilder
    {
        readonly ConsumePipeConfigurator _consumePipeConfigurator;
        readonly IList<IReceiveEndpointSpecification<TBuilder>> _endpointSpecifications;
        readonly PublishPipeConfigurator _publishPipeConfigurator;
        readonly SendPipeConfigurator _sendPipeConfigurator;
        readonly IList<IBusFactorySpecification<TBuilder>> _specifications;

        protected BusFactoryConfigurator()
        {
            _consumePipeConfigurator = new ConsumePipeConfigurator();
            _sendPipeConfigurator = new SendPipeConfigurator();
            _publishPipeConfigurator = new PublishPipeConfigurator();
            _specifications = new List<IBusFactorySpecification<TBuilder>>();
            _endpointSpecifications = new List<IReceiveEndpointSpecification<TBuilder>>();
        }

        protected IConsumePipeFactory ConsumePipeFactory => _consumePipeConfigurator;
        protected ISendPipeFactory SendPipeFactory => _sendPipeConfigurator;
        protected IPublishPipeFactory PublishPipeFactory => _publishPipeConfigurator;

        public void AddPipeSpecification(IPipeSpecification<ConsumeContext> specification)
        {
            _consumePipeConfigurator.AddPipeSpecification(specification);
        }

        public void AddPipeSpecification<T>(IPipeSpecification<ConsumeContext<T>> specification)
            where T : class
        {
            _consumePipeConfigurator.AddPipeSpecification(specification);
        }

        public void ConfigureSend(Action<ISendPipeConfigurator> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            callback(_sendPipeConfigurator);
        }

        public void ConfigurePublish(Action<IPublishPipeConfigurator> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            callback(_publishPipeConfigurator);
        }

        public void AddBusFactorySpecification(IBusFactorySpecification specification)
        {
            _specifications.Add(CreateSpecificationProxy(specification));
        }

        public void AddBusFactorySpecification(IBusFactorySpecification<TBuilder> specification)
        {
            _specifications.Add(specification);
        }

        public void AddReceiveEndpointSpecification(IReceiveEndpointSpecification<TBuilder> specification)
        {
            _endpointSpecifications.Add(specification);
        }

        public virtual IEnumerable<ValidationResult> Validate()
        {
            return _specifications.SelectMany(x => x.Validate())
                .Concat(_endpointSpecifications.SelectMany(x => x.Validate()))
                .Concat(_consumePipeConfigurator.Validate())
                .Concat(_sendPipeConfigurator.Validate())
                .Concat(_publishPipeConfigurator.Validate());
        }

        protected void ApplySpecifications(TBuilder builder)
        {
            foreach (IBusFactorySpecification<TBuilder> configurator in _specifications)
                configurator.Apply(builder);

            foreach (IReceiveEndpointSpecification<TBuilder> configurator in _endpointSpecifications)
                configurator.Apply(builder);
        }

        protected virtual IBusFactorySpecification<TBuilder> CreateSpecificationProxy(IBusFactorySpecification specification)
        {
            return new ConfiguratorProxy(specification);
        }


        class ConfiguratorProxy :
            IBusFactorySpecification<TBuilder>
        {
            readonly IBusFactorySpecification _configurator;

            public ConfiguratorProxy(IBusFactorySpecification configurator)
            {
                _configurator = configurator;
            }

            public IEnumerable<ValidationResult> Validate()
            {
                return _configurator.Validate();
            }

            public void Apply(TBuilder builder)
            {
                _configurator.Apply(builder);
            }
        }
    }
}