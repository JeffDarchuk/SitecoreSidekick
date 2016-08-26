'use strict';
var app = angular.module('app', ['angularUtils.directives.dirPagination', 'ngCookies']);
app.filter("sanitize", [
	'$sce', function($sce) {
		return function(htmlCode) {
			return $sce.trustAsHtml(htmlCode);
		}
	}
]);