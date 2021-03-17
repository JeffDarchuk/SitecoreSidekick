(function () {
	'use strict';

	angular
        .module('app')
        .factory('xcFactory', xcFactory);

	xcFactory.$inject = ['$http'];

	function xcFactory($http) {
		var service = {
			getModels: function () {
				return $http.get("/scs/xc/xcgetmodels.scsvc");
			},
			downloadModel: function (name) {
				return $http.post("/scs/xc/xcdownloadmodel.scsvc", '"'+name+'"');
			}
		};

		return service;

		function getData() { }
	}
})();
