(function () {
	'use strict';

	angular
        .module('app')
        .factory('AppCodeFactory', AppCodeFactory);

	AppCodeFactory.$inject = ['$http'];

	function AppCodeFactory($http) {
		var service = {
			contentDemo: function (content) {
				// Second parameter should be deserializable into the model of the controller action, in this case a string literal in JSON
				// NOTE that it needs to be all lowercase here.
				return $http.post("/scs/AppCode/AppCodecontentdemo.scsvc", "'"+content+"'");
			}
		};

		return service;

		function getData() { }
	}
})();
