(function () {
	'use strict';

	angular
        .module('app')
        .factory('ScsFactory', ScsFactory);

	ScsFactory.$inject = ['$http'];

	function ScsFactory($http) {
		var service = {
			contentTree: function(id, database, server) {
				var data = { "id": id, "database": database, "server": server };
				return $http.post("/scs/contenttree.scsvc", data);
			},
			contentTreeSelectedRelated: function(currentId, selectedId, server) {
				var data = { "currentId": currentId, "selectedId": selectedId, "server": server };
				return $http.post("/scs/contenttreeselectedrelated.scsvc", data);
			},
			valid: function() {
				return $http.get("/scs/scsvalid.scsvc");
			}
		};

		return service;

		function getData() { }
	}
})();