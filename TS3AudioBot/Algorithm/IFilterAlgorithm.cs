// TS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2017  TS3AudioBot contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

namespace TS3AudioBot.Algorithm
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public interface IFilterAlgorithm
	{
		IEnumerable<KeyValuePair<string, T>> Filter<T>(IEnumerable<KeyValuePair<string, T>> list, string filter);
	}

	public sealed class Filter
	{
		public IFilterAlgorithm Current { get; set; }

		public static IFilterAlgorithm DefaultAlgorithm { get; } = Ic3Filter.Instance;
		public static Filter DefaultFilter { get; } = new Filter { Current = DefaultAlgorithm };

		public static R<IFilterAlgorithm> GetFilterByName(string filter)
		{
			R<IFilterAlgorithm> ToR(IFilterAlgorithm obj) => R<IFilterAlgorithm>.OkR(obj);
			switch (filter)
			{
			case "exact": return ToR(ExactFilter.Instance);
			case "substring": return ToR(SubstringFilter.Instance);
			case "ic3": return ToR(Ic3Filter.Instance);
			case "hamming": return ToR(HammingFilter.Instance);
			default: return R.Err;
			}
		}
	}

	/// <summary>Interleaved continuous character chain.</summary>
	internal sealed class Ic3Filter : IFilterAlgorithm
	{
		private Ic3Filter() { }

		public static IFilterAlgorithm Instance { get; } = new Ic3Filter();

		IEnumerable<KeyValuePair<string, T>> IFilterAlgorithm.Filter<T>(IEnumerable<KeyValuePair<string, T>> list, string filter)
		{
			// Convert result to list because it can be enumerated multiple times
			var possibilities = list.Select(t => (Name: t.Key, Value: t.Value, Index: 0)).ToList();
			// Filter matching commands
			foreach (var c in filter.ToLowerInvariant())
			{
				var newPossibilities = (from p in possibilities
										let pos = p.Name.ToLowerInvariant().IndexOf(c, p.Index)
										where pos != -1
										select (p.Name, p.Value, pos + 1)).ToList();
				if (newPossibilities.Count > 0)
					possibilities = newPossibilities;
			}
			// Take command with lowest index
			int minIndex = possibilities.Min(t => t.Index);
			var cmds = possibilities.Where(t => t.Index == minIndex).ToArray();
			// Take the smallest command
			int minLength = cmds.Min(c => c.Name.Length);

			return cmds.Where(c => c.Name.Length == minLength).Select(fi => new KeyValuePair<string, T>(fi.Name, fi.Value));
		}
	}

	internal sealed class ExactFilter : IFilterAlgorithm
	{
		private ExactFilter() { }

		public static IFilterAlgorithm Instance { get; } = new ExactFilter();

		IEnumerable<KeyValuePair<string, T>> IFilterAlgorithm.Filter<T>(IEnumerable<KeyValuePair<string, T>> list, string filter)
		{
			return list.Where(x => x.Key == filter);
		}
	}

	internal sealed class HammingFilter : IFilterAlgorithm
	{
		private HammingFilter() { }

		public static IFilterAlgorithm Instance { get; } = new HammingFilter();

		IEnumerable<KeyValuePair<string, T>> IFilterAlgorithm.Filter<T>(IEnumerable<KeyValuePair<string, T>> list, string filter)
		{
			throw new System.NotImplementedException();
		}
	}

	internal sealed class SubstringFilter : IFilterAlgorithm
	{
		private SubstringFilter() { }

		public static IFilterAlgorithm Instance { get; } = new SubstringFilter();

		IEnumerable<KeyValuePair<string, T>> IFilterAlgorithm.Filter<T>(IEnumerable<KeyValuePair<string, T>> list, string filter)
		{
			var result = list.Where(x => x.Key.StartsWith(filter));
			using (var enu = result.GetEnumerator())
			{
				if (!enu.MoveNext())
					yield break;
				yield return enu.Current;
				if (enu.Current.Key == filter)
					yield break;
				while (enu.MoveNext())
					yield return enu.Current;
			}
		}
	}
}
