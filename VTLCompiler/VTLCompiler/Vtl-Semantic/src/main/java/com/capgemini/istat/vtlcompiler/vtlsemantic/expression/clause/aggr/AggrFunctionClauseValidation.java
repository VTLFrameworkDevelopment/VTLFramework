package com.capgemini.istat.vtlcompiler.vtlsemantic.expression.clause.aggr;

import com.capgemini.istat.vtlcompiler.vtlcommon.model.KeyVariables;
import com.capgemini.istat.vtlcompiler.vtlcommon.model.dataset.VtlComponent;
import com.capgemini.istat.vtlcompiler.vtlcommon.model.operator.OperatorValidation;
import com.capgemini.istat.vtlcompiler.vtlcommon.model.semantic.ResultExpression;
import com.capgemini.istat.vtlcompiler.vtlcommon.model.vtl.expression.VtlExpression;
import com.capgemini.istat.vtlcompiler.vtlcommon.model.vtl.expression.clause.aggr.VtlAggrFunctionClauseExpression;
import com.capgemini.istat.vtlcompiler.vtlsemantic.SemanticFactory;
import com.capgemini.istat.vtlcompiler.vtlsemantic.service.result.FunctionResultService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.annotation.Scope;
import org.springframework.stereotype.Component;

import java.util.LinkedList;
import java.util.Map;

@Component
@Scope("prototype")
public class AggrFunctionClauseValidation implements OperatorValidation {
    private VtlAggrFunctionClauseExpression vtlAggrFunctionClauseExpression;
    private SemanticFactory semanticFactory;
    private FunctionResultService functionResultService;


    @Override
    public void setVtlExpression(VtlExpression vtlExpression) {
        this.vtlAggrFunctionClauseExpression = (VtlAggrFunctionClauseExpression) vtlExpression;
    }

    @Autowired
    public void setFunctionResultService(FunctionResultService functionResultService) {
        this.functionResultService = functionResultService;
    }

    @Autowired
    public void setSemanticFactory(SemanticFactory semanticFactory) {
        this.semanticFactory = semanticFactory;
    }

    @Override
    public LinkedList<ResultExpression> resolve(Map<KeyVariables, Object> variables) throws Exception {
        LinkedList<ResultExpression> resultExpressions = new LinkedList<>();
        LinkedList<ResultExpression> resultAggrFunction = semanticFactory.checkSemantic(vtlAggrFunctionClauseExpression.getVtlAggrComp(), variables);
        VtlComponent vtlComponent = resultAggrFunction.getFirst().getResultComponent();

        ResultExpression resultExpression =
                functionResultService.assignComponentNameAndRole(
                        vtlComponent,
                        vtlAggrFunctionClauseExpression.getVtlcomponentRole(),
                        vtlAggrFunctionClauseExpression.getVtlComponentId().getComponentName()
                );

        resultExpression.setCommand(vtlAggrFunctionClauseExpression.getCommand());
        resultExpressions.add(resultExpression);
        return resultExpressions;
    }
}
