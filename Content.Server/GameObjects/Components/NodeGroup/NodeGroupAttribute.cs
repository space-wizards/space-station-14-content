﻿using System;

namespace Content.Server.GameObjects.Components.NodeGroup
{
    /// <summary>
    ///     Associates a <see cref="INodeGroup"/> implementation with a <see cref="NodeGroupID"/>.
    ///     This is used to gurantee all <see cref="INode"/>s of the same <see cref="INode.NodeGroupID"/>
    ///     have the same type of <see cref="INodeGroup"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class NodeGroupAttribute : Attribute
    {
        public NodeGroupID[] NodeGroupIDs { get; }

        public NodeGroupAttribute(params NodeGroupID[] nodeGroupTypes)
        {
            NodeGroupIDs = nodeGroupTypes;
        }
    }
}
