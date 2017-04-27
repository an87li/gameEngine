﻿using System;

namespace Noise
{
    public sealed class Cache : ModuleBase
    {
       private readonly CacheValue cache2D = new CacheValue();

       private readonly CacheValue cache3D = new CacheValue();

       private readonly CacheValue cache4D = new CacheValue();

       private readonly CacheValue cache6D = new CacheValue();

        public Cache(ModuleBase source)
        {
            this.Source = source;
        }

        public ModuleBase Source { get; set; }

        public override Double Get(Double x, Double y)
        {
            if (!this.cache2D.IsValid || this.cache2D.X != x || this.cache2D.Y != y)
            {
                this.cache2D.X = x;
                this.cache2D.Y = y;
                this.cache2D.IsValid = true;
                this.cache2D.Value = this.Source.Get(x, y);
            }
            return this.cache2D.Value;
        }

        public override Double Get(Double x, Double y, Double z)
        {
            if (!this.cache3D.IsValid || this.cache3D.X != x || this.cache3D.Y != y || this.cache3D.Z != z)
            {
                this.cache3D.X = x;
                this.cache3D.Y = y;
                this.cache3D.Z = z;
                this.cache3D.IsValid = true;
                this.cache3D.Value = this.Source.Get(x, y, z);
            }
            return this.cache3D.Value;
        }

        public override Double Get(Double x, Double y, Double z, Double w)
        {
            if (!this.cache4D.IsValid || this.cache4D.X != x || this.cache4D.Y != y || this.cache4D.Z != z || this.cache4D.W != w)
            {
                this.cache4D.X = x;
                this.cache4D.Y = y;
                this.cache4D.Z = z;
                this.cache4D.W = w;
                this.cache4D.IsValid = true;
                this.cache4D.Value = this.Source.Get(x, y, z, w);
            }
            return this.cache4D.Value;
        }

        public override Double Get(Double x, Double y, Double z, Double w, Double u, Double v)
        {
            if (!this.cache6D.IsValid || this.cache6D.X != x || this.cache6D.Y != y || this.cache6D.Z != z || this.cache6D.W != w || this.cache6D.U != u || this.cache6D.V != v)
            {
                this.cache6D.X = x;
                this.cache6D.Y = y;
                this.cache6D.Z = z;
                this.cache6D.W = w;
                this.cache6D.U = u;
                this.cache6D.V = v;
                this.cache6D.IsValid = true;
                this.cache6D.Value = this.Source.Get(x, y, z, w, u, v);
            }
            return this.cache6D.Value;
        }
    }
}