(function () {
	'use strict';

	angular
        .module('app')
        .factory('csFactory', csFactory);

	csFactory.$inject = ['$http'];

	function csFactory($http) {
		var service = {

			query: function (query) {
				return $http.post("/scs/cs/csquery.scsvc", "\""+query+"\"");
			}
		};

		return service;

		function getData() { }
	}
})();