using HarmonyLib;

namespace DeepSpaceChinese;

/// <summary>
/// The game compares SignalMessage arrays by value, but its GetHashCode still hashes the
/// array reference. ContactTransmissions stores those messages in a Dictionary, so a freshly
/// compiled message can never find an equal serialized key. Resolve the tiny ending table by
/// value at its real call site instead of depending on that broken hash contract.
/// </summary>
internal static class ContactTransmissionCompatibility
{
    internal static SignalMessage ResolveResponseForTests(TransmissionPair[] pairs,
        SignalMessage genericResponse, SignalMessage playerTransmission) =>
        ResolveResponse(pairs, genericResponse, playerTransmission);

    internal static SignalMessage ResolveResponse(TransmissionPair[] pairs,
        SignalMessage genericResponse, SignalMessage playerTransmission)
    {
        if (pairs != null)
        {
            foreach (TransmissionPair pair in pairs)
            {
                if (pair != null && SignalsEqual(
                        pair.playerTransmission.signals, playerTransmission.signals))
                    return pair.responseTransmission;
            }
        }
        return genericResponse;
    }

    private static bool SignalsEqual(int[] left, int[] right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left == null || right == null || left.Length != right.Length)
            return false;
        for (int index = 0; index < left.Length; index++)
        {
            if (left[index] != right[index])
                return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(ContactTransmissions), nameof(ContactTransmissions.GetResponse))]
internal static class ContactTransmissionsGetResponsePatch
{
    private static bool Prefix(ContactTransmissions __instance,
        SignalMessage playerTransmission, ref SignalMessage __result)
    {
        __result = ContactTransmissionCompatibility.ResolveResponse(
            __instance.transmissionPairs, __instance.genericResponse, playerTransmission);
        return false;
    }
}
