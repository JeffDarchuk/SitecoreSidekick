(function () {
	'use strict';

	angular
        .module('app')
        .factory('CMfactory', CMfactory);

	CMfactory.$inject = ['$http'];

	function CMfactory($http) {
		var service = {

			contentTree: function (id, database, server) {
				var data = { "id": id, "database": database, "server": server };
				return $http.post("/scs/cmcontenttree.scsvc", data);
			},
			contentTreeGetItem: function (id, database, server) {
				var data = { "id": id, "database": database, "server": server };
				return $http.post("/scs/cmcontenttreegetitem.scsvc", data);
			},
			contentTreePullItem: function (id, database, server, children, overwrite, pullParent, mirror) {
				var data = { "id": id, "database": database, "server": server, "children": children, "overwrite": overwrite, "pullParent": pullParent, "mirror": mirror };
				return $http.post("/scs/cmcontenttreepullitem.scsvc", data);
			},
			contentTreeServerList: function () {
				return $http.get("/scs/cmserverlist.scsvc");
			},
			operationStatus: function (operationId, lineNumber) {
				var data = { "operationId": operationId, "lineNumber": lineNumber };
				return $http.post("/scs/cmopeartionstatus.scsvc", data);
			}


		};

		return service;

		function getData() { }
	}
})();