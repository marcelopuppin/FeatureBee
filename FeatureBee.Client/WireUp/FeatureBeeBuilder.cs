﻿namespace FeatureBee.WireUp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    using FeatureBee.ConfigSection;
    using FeatureBee.Evaluators;
    using FeatureBee.UpdateModes;

    public class FeatureBeeBuilder
    {
        private FeatureBeeBuilder(IFeatureBeeContext context)
        {
            Context = context;
            Context.Evaluators = LoadConditionEvaluators();
        }

        internal static IFeatureBeeContext Context { get; private set; }

        public static FeatureBeeBuilder ForWebApp(Func<HttpContextBase> httpContextFunc)
        {
            return new FeatureBeeBuilder(new WebApplicationContext(httpContextFunc));
        }

        public static FeatureBeeBuilder ForWebApp()
        {
            return new FeatureBeeBuilder(new WebApplicationContext(() => new HttpContextWrapper(HttpContext.Current)));
        }

        public static FeatureBeeBuilder ForWindowsService()
        {
            return new FeatureBeeBuilder(new WindowsApplicationContext());
        }

        public void Use(IFeatureRepository featureRepository = null, List<IConditionEvaluator> conditionEvaluators = null)
        {
            if (featureRepository != null)
            {
                Context.FeatureRepository = featureRepository;
            }

            if (conditionEvaluators != null)
            {
                Context.Evaluators = conditionEvaluators;
            }
        }

        public void UseConfig()
        {
            var config = FeatureBeeConfiguration.GetSection();
            Context.FeatureRepository = UpdateModeFactory.Get(config.Server.UpdateMode, config.Server.Url);

            Context.ShowTrayIconOnPages = config.Tray.ShowTrayIconOnPages;
            Context.TrafficDistributionCookie = config.Settings.TrafficDistributionCookie;
        }

        private static List<IConditionEvaluator> LoadConditionEvaluators()
        {
            var types = typeof (FeatureBeeBuilder).Assembly.GetTypes().Where(TypeIsConditionEvaluator).ToList();
            return types.Select(_ =>
            {
                var constructor = _.GetConstructor(Type.EmptyTypes);
                return constructor != null ? (IConditionEvaluator) constructor.Invoke(null) : null;
            }).ToList();
        }

        private static bool TypeIsConditionEvaluator(Type type)
        {
            return type.GetInterface(typeof (IConditionEvaluator).Name) != null && !type.IsAbstract;
        }
    }
}