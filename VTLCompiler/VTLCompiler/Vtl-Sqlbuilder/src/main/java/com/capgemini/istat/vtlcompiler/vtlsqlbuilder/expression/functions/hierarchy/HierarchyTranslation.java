package com.capgemini.istat.vtlcompiler.vtlsqlbuilder.expression.functions.hierarchy;

import com.capgemini.istat.vtlcompiler.vtlcommon.model.KeyVariables;
import com.capgemini.istat.vtlcompiler.vtlcommon.model.translation.SqlResult;
import com.capgemini.istat.vtlcompiler.vtlcommon.model.vtl.dataset.VtlComponentId;
import com.capgemini.istat.vtlcompiler.vtlcommon.model.vtl.define.ruleset.hierarchical.VtlCodeItem;
import com.capgemini.istat.vtlcompiler.vtlcommon.model.vtl.define.ruleset.hierarchical.VtlHRRule;
import com.capgemini.istat.vtlcompiler.vtlcommon.model.vtl.expression.VtlExpression;
import com.capgemini.istat.vtlcompiler.vtlcommon.model.vtl.expression.hierarchy.VtlHierarchyExpression;
import com.capgemini.istat.vtlcompiler.vtlsqlbuilder.TranslationFactory;
import com.capgemini.istat.vtlcompiler.vtlsqlbuilder.model.OperatorTranslation;
import com.capgemini.istat.vtlcompiler.vtlsqlbuilder.service.sqlresult.ISqlResultService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.annotation.Scope;
import org.springframework.stereotype.Component;

import java.util.HashMap;
import java.util.Map;

@Component
@Scope("prototype")
public class HierarchyTranslation implements OperatorTranslation {
    private VtlHierarchyExpression vtlHierarchyExpression;
    private TranslationFactory translationFactory;
    private ISqlResultService sqlResultService;


    @Override
    public void setVtlExpression(VtlExpression vtlExpression) {
        this.vtlHierarchyExpression = (VtlHierarchyExpression) vtlExpression;
    }

    @Autowired
    public void setTranslationFactory(TranslationFactory translationFactory) {
        this.translationFactory = translationFactory;
    }


    public void setSqlResultService(ISqlResultService sqlResultService) {
        this.sqlResultService = sqlResultService;
    }

    @Override
    public SqlResult translate(Map<KeyVariables, Object> variables) throws Exception {
        SqlResult sqlResult;
        SqlResult datasetResult = translationFactory.translate(vtlHierarchyExpression.getVtlOperandExpression(), variables);
        variables.put(KeyVariables.DATASET_IN_CLAUSE, datasetResult.getSqlDataset());
        for (VtlComponentId vtlComponentId : vtlHierarchyExpression.getComponentIds()) {
            translationFactory.translate(vtlComponentId, variables);
        }
        translationFactory.translate(vtlHierarchyExpression.getRuleComponent(), variables);
        variables.put(KeyVariables.PARAMETER_MAP, vtlHierarchyExpression.getParameterMapping());
        variables.put(KeyVariables.FILTER_CLAUSE, true);

        for (VtlHRRule vtlHRRule : vtlHierarchyExpression.getVtlHierarchicalRuleset().getVtlHRRules()) {
            if (vtlHRRule.getVtlCodeItemRelation().getLeftCondition() != null) {
                variables.put(KeyVariables.PARAMETER_USED, new HashMap<String, VtlComponentId>());
                translationFactory.translate(vtlHRRule.getVtlCodeItemRelation().getLeftCondition(), variables);
                vtlHRRule.getVtlCodeItemRelation().setParameterUsed((Map<String, VtlComponentId>) variables.get(KeyVariables.PARAMETER_USED));
            }
            for (VtlCodeItem vtlCodeItem : vtlHRRule.getVtlCodeItemRelation().getVtlCodeItems()) {
                if (vtlCodeItem.getCondition() != null)
                    translationFactory.translate(vtlCodeItem.getCondition(), variables);
            }
        }

        Map<String, VtlExpression> parameterMap = (Map<String, VtlExpression>) variables.get(KeyVariables.PARAMETER_MAP);
        variables.remove(KeyVariables.PARAMETER_MAP);
        variables.remove(KeyVariables.FILTER_CLAUSE);
        translationFactory.translate(vtlHierarchyExpression.getCustomPivot(), variables);
        variables.put(KeyVariables.FILTER_CLAUSE, true);
        variables.put(KeyVariables.PARAMETER_MAP, parameterMap);
        sqlResult = sqlResultService.hierarchy(vtlHierarchyExpression);
        variables.remove(KeyVariables.PARAMETER_MAP);
        variables.remove(KeyVariables.FILTER_CLAUSE);
        return sqlResult;
    }
}
