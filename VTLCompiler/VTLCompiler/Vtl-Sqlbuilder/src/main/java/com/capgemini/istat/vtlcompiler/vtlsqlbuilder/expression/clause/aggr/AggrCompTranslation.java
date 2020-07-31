package com.capgemini.istat.vtlcompiler.vtlsqlbuilder.expression.clause.aggr;

import com.capgemini.istat.vtlcompiler.vtlcommon.model.KeyVariables;
import com.capgemini.istat.vtlcompiler.vtlcommon.model.translation.SqlComponent;
import com.capgemini.istat.vtlcompiler.vtlcommon.model.translation.SqlDataset;
import com.capgemini.istat.vtlcompiler.vtlcommon.model.translation.SqlResult;
import com.capgemini.istat.vtlcompiler.vtlcommon.model.vtl.expression.VtlExpression;
import com.capgemini.istat.vtlcompiler.vtlcommon.model.vtl.expression.clause.aggr.VtlAggrComp;
import com.capgemini.istat.vtlcompiler.vtlsqlbuilder.TranslationFactory;
import com.capgemini.istat.vtlcompiler.vtlsqlbuilder.model.OperatorTranslation;
import com.capgemini.istat.vtlcompiler.vtlsqlbuilder.service.sqlresult.ISqlResultService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.annotation.Scope;
import org.springframework.stereotype.Component;

import java.util.Map;

@Component
@Scope("prototype")
public class AggrCompTranslation implements OperatorTranslation {
    private VtlAggrComp vtlAggrComp;
    private TranslationFactory translationFactory;
    private ISqlResultService sqlResultService;

    @Override
    public void setVtlExpression(VtlExpression vtlExpression) {
        this.vtlAggrComp = (VtlAggrComp) vtlExpression;
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

        if (vtlAggrComp.getVtlExpression() != null) {
            sqlResult = translationFactory.translate(vtlAggrComp.getVtlExpression(), variables);
        } else {
            sqlResult = new SqlResult();
            SqlComponent sqlComponent = new SqlComponent();
            sqlResult.setSqlComponent(sqlComponent);
            sqlResult.setSqlDataset((SqlDataset) variables.get(KeyVariables.DATASET_IN_CLAUSE));
        }
        sqlResult = sqlResultService.applyAggregateFunction(vtlAggrComp, vtlAggrComp.getVtlExpression() != null, sqlResult);


        return sqlResult;
    }
}