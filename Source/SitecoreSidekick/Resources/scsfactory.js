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
				return $http.post("/scs/platform/contenttree.scsvc", data);
			},
			contentTreeSelectedRelated: function(selectedIds, server, database) {
				var data = { "selectedIds": selectedIds, "server": server, "database": database };
				return $http.post("/scs/platform/contenttreeselectedrelated.scsvc", data);
			},
			valid: function() {
				return $http.get("/scs/platform/scsvalid.scsvc");
			}
		};

		return service;

		function getData() { }
	}
})();