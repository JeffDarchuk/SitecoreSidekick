(function () {
	'use strict';

	angular
        .module('app')
        .factory('jmFactory', jmFactory);

	jmFactory.$inject = ['$http'];

	function jmFactory($http) {
		var service = {
			getJobs: function () {
				// Second parameter should be deserializable into the model of the controller action, in this case a string literal in JSON
				// NOTE that it needs to be all lowercase here.
				return $http.post("/scs/jm/jmgetjobs.scsvc");
			},
			cancelJob: function (name) {
				// Second parameter should be deserializable into the model of the controller action, in this case a string literal in JSON
				// NOTE that it needs to be all lowercase here.
				return $http.post("/scs/jm/jmcanceljob.scsvc", "\""+name.replace("\"", "\\\"")+"\"");
			}
		};

		return service;

		function getData() { }
	}
})();
