using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSBase.Common
{
    public enum EMLogicControl
    {
        emLogicSuccess = 0x01,
        emLogicFailed = 0x02,

        emLogicContine = 0x04,
        emLogicBreak = 0x08,
    }

    public enum EMCompareOp
    {
        emUnknown,

        emLess,
        emLessEqual,

        emEqual,
        emEqualSingle,
        emNotEqual,

        emAbove,
        emAboveEqual
    }

    public enum EMLogicOp
    {
        emUnknown,

        emAnd,       
        emOr,
        emNot
    }

    public class STUConditionUnit<TyKey, TyValue>
    {
        public TyKey tyKey;
        public TyValue tyValue;
        public EMCompareOp emLogicOp;

        public STUConditionUnit(TyKey tyKeyIn, TyValue tyValueIn, EMCompareOp emLogicOpIn)
        {
            tyKey = tyKeyIn;
            tyValue = tyValueIn;
            emLogicOp = emLogicOpIn;
        }
    }

    public class STUConditionGroup<TyKey, TyValue>
    {
        public List<STUConditionUnit<TyKey, TyValue>> lsConditionGroup;
        public EMLogicOp emLogicInGroup;

        public STUConditionGroup(List<STUConditionUnit<TyKey, TyValue>> lsConditionGroupIn, EMLogicOp emLogicInGroupIn)
        {
            lsConditionGroup = lsConditionGroupIn;
            emLogicInGroup = emLogicInGroupIn;
        }
    }


    public class STUCondition<TyKey, TyValue>
    {
        public List<STUConditionGroup<TyKey, TyValue>> lsConditionGroups;
        public EMLogicOp emLogicBetweenGroup;

        public STUCondition(List<STUConditionGroup<TyKey, TyValue>> lsConditionGroupsIn, EMLogicOp emLogicBetweenGroupIn)
        {
            lsConditionGroups = lsConditionGroupsIn;
            emLogicBetweenGroup = emLogicBetweenGroupIn;
        }
    }

#if false
    public struct STUConditionGroupUnit<TyKey, TyValue>
    {
        public STUConditionUnit<TyKey, TyValue> stuLogicUnit;
        public EMLogicOp emNextUnitConnectLogicOp;

        public STUConditionGroupUnit(STUConditionUnit<TyKey, TyValue> stuLogicUnitIn, EMLogicOp emNextUnitConnectLogicOpIn)
        {
            stuLogicUnit = stuLogicUnitIn;
            emNextUnitConnectLogicOp = emNextUnitConnectLogicOpIn;
        }
    }

    public struct STULogicCondition<TyKey, TyValue>
    {
        // Content: group items
        public List<STUConditionGroupUnit<TyKey, TyValue>> lsGroupItems;
        public EMLogicOp emNextGroupConnectLogicOp;

        // Sub tree: sub groups
        public List<STULogicCondition<TyKey, TyValue>> lsSubGroups;
    }
#endif
}
