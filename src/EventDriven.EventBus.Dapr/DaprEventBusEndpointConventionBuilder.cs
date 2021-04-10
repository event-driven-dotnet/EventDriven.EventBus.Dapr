using System;
using Microsoft.AspNetCore.Builder;

namespace EventDriven.EventBus.Dapr
{
    /// <summary>
    /// Builds conventions that will be used for customization of Hub <see cref="EndpointBuilder"/> instances.
    /// </summary>
    public sealed class DaprEventBusEndpointConventionBuilder : IEndpointConventionBuilder
    {
        private readonly IEndpointConventionBuilder _endpointConventionBuilder;

        
        internal DaprEventBusEndpointConventionBuilder(IEndpointConventionBuilder endpointConventionBuilder)
        {
            _endpointConventionBuilder = endpointConventionBuilder;
        }

        /// <summary>
        /// Adds the specified convention to the builder. Conventions are used to customize <see cref="EndpointBuilder"/> instances.
        /// </summary>
        /// <param name="convention">The convention to add to the builder.</param>
        public void Add(Action<EndpointBuilder> convention)
        {
            _endpointConventionBuilder.Add(convention);
        }
    }
}