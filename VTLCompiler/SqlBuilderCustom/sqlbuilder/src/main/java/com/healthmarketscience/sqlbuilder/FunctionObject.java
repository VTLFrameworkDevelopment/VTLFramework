/*
Copyright (c) 2008 Health Market Science, Inc.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

package com.healthmarketscience.sqlbuilder;

import com.healthmarketscience.common.util.AppendableExt;
import com.healthmarketscience.sqlbuilder.dbspec.Function;

import java.io.IOException;
import java.io.Serializable;



/**
 * Outputs the "simple" name of a function.
 *
 * @author James Ahlborn
 */
class FunctionObject extends SqlObject implements Serializable
{
  protected Function _function;
    
  FunctionObject(Function function) {
    _function = function;
  }

  @Override
  protected void collectSchemaObjects(ValidationContext vContext) {
  }
  
  @Override
  public void appendTo(AppendableExt app) throws IOException {
    app.append(_function.getFunctionNameSQL());
  }
}
