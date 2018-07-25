(function () {
	'use strict';

	angular
        .module('app')
        .factory('hgFactory', hgFactory);

	hgFactory.$inject = ['$http'];

	function hgFactory($http) {
		var service = {
			contentTree: function (id, database, server) {
				var data = { "id": id, "database": database, "server": server };
				return $http.post("/scs/hg/hgcontenttree.scsvc", data);
			},
			getTemplates: function () {

				return $http.get("/scs/hg/hggettemplates.scsvc");
			},
			uploadTemplate: function (formData) {
				return $http.put("/scs/hg/hguploadtemplate.scsvc", formData)
			},
			getTargets: function (template) {
				return $http.post("/scs/hg/hggettargets.scsvc", "'" + template + "'")
			},
			getProperties: function (template, target) {
				return $http.post("/scs/hg/hggetproperties.scsvc", {"Template": template, "Target": target });
			},
			removeTemplate: function (template) {
				return $http.post("/scs/hg/hgremovetemplate.scsvc", "'" + template + "'");
			},
			execute: function (properties, template, target) {
				return $http.post("/scs/hg/hgexecute.scsvc", { "Properties": properties, "Template": template, "Target": target});
			},
			getProjects: function (solutionPath) {
				return $http.post("/scs/hg/hggetprojects.scsvc", "\"" + solutionPath.split("\\").join("\\\\") + "\"");
			},
			getControllers: function (projectPath) {
				return $http.post("/scs/hg/hggetcontrollers.scsvc", "\"" + projectPath.split("\\").join("\\\\") + "\"");
			},
			submitTargetProp: function(target, template, propertyId, value) {
				return $http.post("/scs/hg/hgsubmittargetproperty.scsvc", { "TargetId": target, "Template": template, "PropertyId": propertyId, "Value": value });

			}
		};

		return service;

		function getData() { }
	}
})();