(function () {
	'use strict';

	angular
        .module('app')
        .factory('ECFactory', ECFactory);

	ECFactory.$inject = ['$http'];

	function ECFactory($http) {
		var service = {

			getLocations: function () {
				return $http.get("/scs/ecgetcommonlocations.json");
			},
			getItemHistory: function () {
				return $http.get("/scs/ecgetitemhistory.json");
			},
			getRelatedItems: function () {
				return $http.get("/scs/ecgetrelated.json");
			},
			getReferrersItems: function () {
				return $http.get("/scs/ecgetreferrers.json");
			}
		};

		return service;

		function getData() { }
	}
})();