(function () {
	'use strict';

	angular
        .module('app')
        .factory('ECFactory', ECFactory);

	ECFactory.$inject = ['$http'];

	function ECFactory($http) {
		var service = {

			getLocations: function () {
				return $http.get("/scs/ec/getcommonlocations.json");
			},
			getRelatedItems: function () {
				return $http.get("/scs/ec/getrelated.json");
			},
			getReferrersItems: function () {
				return $http.get("/scs/ec/getreferrers.json");
			}
		};

		return service;

		function getData() { }
	}
})();