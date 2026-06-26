using RoslynTestKit;
using NamingPatternTarget = ALCops.LinterCop.Analyzers.NamingPattern.NamingTarget;
using NamingPatternConfig = ALCops.LinterCop.Analyzers.NamingPattern.NamingPatternConfig;
using NamingPatternSetting = ALCops.Common.Settings.NamingPattern;

namespace ALCops.LinterCop.Test
{
    public class NamingPatternSettings : NavCodeAnalysisBase
    {
        // Verifies that each target resolves to the pattern of the closest ancestor
        // that has an override. When all targets have overrides, each resolves its own.
        [Test]
        [TestCase(NamingPatternTarget.LocalVariable, "LocalVariable")]
        [TestCase(NamingPatternTarget.GlobalVariable, "GlobalVariable")]
        [TestCase(NamingPatternTarget.Parameter, "Parameter")]
        [TestCase(NamingPatternTarget.VarParameter, "VarParameter")]
        public async Task InheritanceResolvesOwnOverrideFirst(
            NamingPatternTarget target, string expectedSourceTarget)
        {
            var config = new NamingPatternConfig(GetOverridesForAllTargets());

            var expected = AllowPatternFor(expectedSourceTarget);
            var actual = config.GetPatterns(target).AllowPatternString;

            Assert.That(actual, Is.EqualTo(expected));
        }

        // Verifies fallback: removing a target's own override
        // causes it to inherit from the proper ancestor that has one.
        [Test]
        [TestCase(NamingPatternTarget.LocalVariable, "Variable")]
        [TestCase(NamingPatternTarget.GlobalVariable, "Variable")]
        [TestCase(NamingPatternTarget.Parameter, "LocalVariable")]
        [TestCase(NamingPatternTarget.VarParameter, "Parameter")]
        public async Task InheritanceFallsBackToNearestAncestorWhenOwnOverrideMissing(
            NamingPatternTarget target, string expectedSourceTarget)
        {
            // Overrides for all targets except the target under test itself, which is the nearest ancestor to Parameter and VarParameter.
            var overrides = GetOverridesForAllTargets(target.ToString());
            var config = new NamingPatternConfig(overrides);

            var expected = AllowPatternFor(expectedSourceTarget);
            var actual = config.GetPatterns(target).AllowPatternString;

            Assert.That(actual, Is.EqualTo(expected));
        }

        // Verifies the full chain: only Variable has an override -> all descendants inherit it.
        [Test]
        [TestCase(NamingPatternTarget.LocalVariable)]
        [TestCase(NamingPatternTarget.GlobalVariable)]
        [TestCase(NamingPatternTarget.Parameter)]
        [TestCase(NamingPatternTarget.VarParameter)]
        public async Task InheritanceFallsBackToVariableWhenOnlyVariableHasOverride(
            NamingPatternTarget target)
        {
            var overrides = new Dictionary<string, NamingPatternSetting>
            {
                ["Variable"] = new NamingPatternSetting { AllowPattern = AllowPatternFor("Variable") }
            };

            var config = new NamingPatternConfig(overrides);

            var expected = AllowPatternFor("Variable");
            var actual = config.GetPatterns(target).AllowPatternString;

            Assert.That(actual, Is.EqualTo(expected));
        }

        private static string AllowPatternFor(string targetName) => $"{targetName}_Allow";
        private static string DisallowPatternFor(string targetName) => $"{targetName}_Disallow";

        private static Dictionary<string, NamingPatternSetting> GetOverridesForAllTargets(params string[] toSkip)
        {
            var overrides = new Dictionary<string, NamingPatternSetting>();

            foreach (NamingPatternTarget t in Enum.GetValues(typeof(NamingPatternTarget)))
            {
                var name = t.ToString();

                if (!toSkip.Contains(name))
                {
                    overrides[name] = new NamingPatternSetting
                    {
                        AllowPattern = AllowPatternFor(name),
                        DisallowPattern = DisallowPatternFor(name)
                    };
                }
            }

            return overrides;
        }
    }
}
