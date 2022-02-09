angular.module("umbraco.install").controller("Umbraco.Install.ProjectMetricsController", function ($scope, $http, installerService){

  $scope.validateAndInstall = function () {
    installerService.install();
  };


  $scope.goBack = function () {
    installerService.goToPreviousView();
  }

});
