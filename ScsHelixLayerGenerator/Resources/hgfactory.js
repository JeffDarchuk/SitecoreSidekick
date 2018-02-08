(function () {
	'use strict';

	angular
        .module('app')
        .factory('hgFactory', hgFactory);

	hgFactory.$inject = ['$http'];

	function hgFactory($http) {
		var service = {
			getTemplates: function () {

				return $http.get("/scs/hg/hggettemplates.scsvc");
			},
			uploadTemplate: function (formData) {
				return $http.put("/scs/hg/hguploadtemplate.scsvc", formData)
			},
			getTargets: function (template) {
				return $http.post("/scs/hg/hggettargets.scsvc", "'" + template + "'")
			},
			getProperties: function (template) {
				return $http.post("/scs/hg/hggetproperties.scsvc","'"+template+"'");
			},
			removeTemplate: function (template) {
				return $http.post("/scs/hg/hgremovetemplate.scsvc", "'" + template + "'");
			},
			execute: function (properties, template, target) {
				return $http.post("/scs/hg/hgexecute.scsvc", { "Properties": properties, "Template": template, "Target": target});
			}
		};

		return service;

		function getData() { }
	}
})();