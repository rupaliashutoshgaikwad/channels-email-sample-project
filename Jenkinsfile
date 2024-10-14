import groovy.json.JsonOutput

def dotnet_ci_image="channels-email-ci-dotnetcore"
def dev_aws_account_id="300813158921"
def test_aws_account_id="265671366761"
def staging_aws_account_id="545209810301"
def prod_aws_account_id="737494165703"

pipeline {
    agent {
        docker {
            image "${dotnet_ci_image}:latest"
            args "--privileged -u root -v /var/run/docker.sock:/var/run/docker.sock"
            alwaysPull true
            customWorkspace "sample-project/${BUILD_ID}+${BUILD_TIMESTAMP}"
            reuseNode false
            registryUrl 'https://369498121101.dkr.ecr.us-west-2.amazonaws.com'
            registryCredentialsId 'ecr:us-west-2:ServiceAccess-jenkins-ecr_369498121101'
        }
    }
    options {
        buildDiscarder(logRotator(numToKeepStr: '5'))
    }
    environment {
        TAGS="owner=channels,product=cloudemail"
        IGNORE_NORMALISATION_GIT_HEAD_MOVE=1
        PROJECT_VERSION=""
        PROJECT_VERSION_NO_TAG=""
        BRANCHNAME="${env.BRANCH_NAME}"
        GITHUB_TOKEN=credentials("GITHUB_TOKEN_INCONTACTBUILDUSER")
        SCRIPTS_REPO_URI_WTOKEN="https://oauth2:${GITHUB_TOKEN}@github.com/inContact/channels-cicd.git"
        BUILD_NOTIFY_WEBHOOK=credentials("channels-1-build-notifications-webhook-url")
    }
    stages {
        stage("Sonar Quality Gate PR") {
            when {
                changeRequest()
                expression {
                    "${currentBuild.currentResult}" == 'SUCCESS'
                }
            }
            environment {
                SOLUTION="src"
                SOLUTION_NAME="CloudEmail.SampleProject.API"
                PROJECTS='["CloudEmail.SampleProject.API.Tests"]'
                SONAR_KEY=credentials("SONAR_SCANNER_TOKEN")
                SONAR_URL=credentials("SONAR_SCANNER_URL")
                SONAR_PROJECT_NAME="channels-email-sample-project"
                NUGET_CONFIG="src/.nuget/NuGet.Config"
                CONFIGURATION="Debug"
                VERBOSE="true"
                PULLREQUEST="true"
                PULLREQUEST_ID="$CHANGE_ID"
                GITHUB_REPO_PATH="inContact/channels-email-sample-project"
                GITHUB_ACCESS_TOKEN="$GITHUB_TOKEN"
            }
            steps {
                setupVersionAndScripts()
                runQualityGate()
            }
        }
        stage("Build & Quality Gate") {
            when {
                anyOf {
                    branch "develop"
                    branch "master"
                }
                expression {
                    "${currentBuild.currentResult}" == 'SUCCESS'
                }
            }
            environment {
                SOLUTION="src"
                SOLUTION_NAME="CloudEmail.SampleProject.API"
                PROJECTS='["CloudEmail.SampleProject.API.Tests"]'
                PROJECTS_DIR=""
                SONAR_KEY=credentials("SONAR_SCANNER_TOKEN")
                SONAR_URL=credentials("SONAR_SCANNER_URL")
                SONAR_PROJECT_NAME="channels-email-sample-project"
                NUGET_CONFIG="src/.nuget/NuGet.Config"
                CONFIGURATION="Debug"
                VERBOSE="true"
            }
            steps {
                setupVersionAndScripts()
                runQualityGate()
                waitForQualityGateResults()
				generateXUnitTestResults()
                publishTestResultsToJira()
                githubRelease()
            }
        }
        stage("Publish to Myget") {
            when {
                expression {
                    "${currentBuild.currentResult}" == 'SUCCESS'
                }
                not {
                    changeRequest()
                }
            }
            environment {
                NUGET_CONFIG="src/.nuget/NuGet.Config"
                PROJECT="src/CloudEmail.SampleProject.API.Client"
                CONFIGURATION="Debug"
                NUGET_KEY=credentials("myget-key")
                NUGET_SOURCE="https://www.myget.org/F/incontact-dev/api/v2/package"
            }
            steps {
                setupVersionAndScripts()
                buildForMyget()
                publishToMyget("CloudEmail.SampleProject.API.Client", "CloudEmail.SampleProject.API.Client")
                publishToMyget("CloudEmail.SampleProject.API.Models", "CloudEmail.SampleProject.API.Models")
            }
        }
        stage("Create ECR Repository"){
            when {
                branch "develop"
                expression {
                    "${currentBuild.currentResult}" == 'SUCCESS'
                }
            }
            environment {
                STACK_NAME="channels-email-sample-project-ecr"
                STACK_FILE="cloudformation/ecr-repository.yml"
                AWS_DEFAULT_REGION="us-west-2"
            }
            steps {
                withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
                    withAWS(role:'pipeline-do-channels-email-SampleProject-deploy', roleAccount:"$dev_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'dev-send-email-ecr-pipeline'){
                        setupVersionAndScripts()
                        deployCf()
                    }
                }
            }
        }
        stage("Pre-Deploy") {
            when {
                expression {
                    "${currentBuild.currentResult}" == 'SUCCESS'
                }
            }
            parallel {
                stage("Build Docker Dev") {
                    when {
                        branch "develop"
                    }
                    steps {
                        setupVersionAndScripts()
                        buildDocker("channels-email-sample-project", "--pull .")
                    }
                    post("Push to ECR Alpha") {
                        success {
                            withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
                                withAWS(role:'pipeline-do-channels-email-SampleProject-deploy', roleAccount:"$dev_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'dev-push-sample-project-pipeline'){
                                    pushDocker("300813158921.dkr.ecr.us-west-2.amazonaws.com", "channels-email-sample-project", "alpha", false)
                                }
                            }
                        }
                    }
                }
                stage("Build Automation Docker Dev") {
                    when {
                        branch "develop"
                    }
                    steps {
                        setupVersionAndScripts()
                        buildDocker("channels-email-sample-project-automation", "-f src/CloudEmail.SampleProject.API.Automation/Dockerfile --pull . --build-arg ASPNETCORE_ENVIRONMENT=Development")
                    }
                    post("Push Automation to ECR Dev") {
                        success {
                            withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
                                withAWS(role:'pipeline-do-channels-email-SampleProject-deploy', roleAccount:"$dev_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'dev-push-sample-project-automation-pipeline'){
                                    pushDocker("300813158921.dkr.ecr.us-west-2.amazonaws.com", "channels-email-sample-project-automation", "alpha", true)
                                }
                            }
                        }
                    }
                }
                stage("Build Docker Master") {
                    when {
                        branch "master"
                    }
                    steps {
                        setupVersionAndScripts()
                        buildDocker("channels-email-sample-project", "--pull .")
                    }
                    post("Push to ECR") {
                        success {
                            withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
                                withAWS(role:'pipeline-do-channels-email-sendemailapi-deploy', roleAccount:"$dev_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'dev-push-sample-project-master-pipeline'){
                                    pushDocker("300813158921.dkr.ecr.us-west-2.amazonaws.com", "channels-email-sample-project", "", true)
                                }
                            }
                        }
                    }
                }
            }
        }
        stage("Deploy Cloudformation Infrastructure to Dev") {
            when {
                branch "develop"
                expression {
                    "${currentBuild.currentResult}" == 'SUCCESS'
                }
            }
            environment {
                STACK_NAME="channels-email-sample-project-supplemental-infrastructure"
                STACK_FILE="cloudformation/supplemental-infrastructure.yml"
                AWS_DEFAULT_REGION="us-west-2"
            }
            steps {
                withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
                    withAWS(role:'pipeline-do-channels-email-sendemailapi-deploy', roleAccount:"$dev_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'dev-sample-project-infra-pipeline'){
                        setupVersionAndScripts()
                        deployCf()
                    }
                }
            }
        }
        stage("Deploy to Kubernetes Cluster to Dev") {
            when {
                branch "develop"
                expression {
                    "${currentBuild.currentResult}" == 'SUCCESS'
                }
            }
            environment {
                POD_SERVICE_ROLE="do-channels-email-sample-project-role"
                ASPNETCORE_ENVIRONMENT="Development"
                AWS_ACCOUNT_ID="${dev_aws_account_id}"
                AWS_DEFAULT_REGION="us-west-2"
                EXTERNAL_URL="sample-project-na1.dev.inucn.com"
                MIN_REPLICAS="2"
                MAX_REPLICAS="4"
            }
            steps {
                withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
                    withAWS(role:'pipeline-do-channels-email-sendemailapi-deploy', roleAccount:"$dev_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'dev-sample-project-deploy-pipeline'){
                        setupVersionAndScripts()
                        deployK8s()
                    }
                }
            }
        }
        stage("Run Automation IC-Dev") {
            when {
                branch "develop"
                not {
                    changeRequest()
                }
                expression {
                    "${currentBuild.currentResult}" == 'SUCCESS'
                }
            }
            environment {
                AWS_DEFAULT_REGION="us-west-2"
                POD_SERVICE_ROLE="do-channels-email-sample-project-role"
                ASPNETCORE_ENVIRONMENT="Development"
                AWS_ACCOUNT_ID="${dev_aws_account_id}"
                TEST_RESULTS_XML_BODY=" "
            }
            steps {
                withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
                    withAWS(role:'pipeline-do-channels-email-sendemailapi-deploy', roleAccount:"$dev_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'dev-sample-project-automation-pipeline'){
                        setupVersionAndScripts()
                        runTestsK8s('k8s/sample-project-acceptance-testing.yml.template', 'sample-project-acceptance')
                    }
                }
            }
        }
        stage("Create Pull Request") {
            when {
                allOf {
                    branch "develop"
                    expression {
                        "${currentBuild.currentResult}" == 'SUCCESS'
                    }
                }
            }
            environment {
                CURRENT_VERSION="${PROJECT_VERSION}"
                NEXT_VERSION="${PROJECT_VERSION_NO_TAG}"
            }
            steps {
                setupVersionAndScripts()
                createPullRequest()
            }
        }
        stage("Build Automation Docker Test") {
            when {
                branch "master"
                expression {
                    "${currentBuild.currentResult}" == 'SUCCESS'
                }
            }
            steps {
                setupVersionAndScripts()
                buildDocker("channels-email-sample-project-automation", "-f src/CloudEmail.SampleProject.API.Automation/Dockerfile --pull . --build-arg ASPNETCORE_ENVIRONMENT=Test")
            }
            post("Push Automation to ECR Test") {
                success {
                    withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
                        withAWS(role:'pipeline-to-channels-email-sampleproject-deploy', roleAccount:"$test_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'test-push-sample-project-automation-pipeline'){
                            pushDocker("265671366761.dkr.ecr.us-west-2.amazonaws.com", "channels-email-sample-project-automation", "beta", true)
                        }
                    }
                }
            }
        }
        stage("Deploy Cloudformation Infrastructure to Test") {
            when {
                branch "master"
                expression {
                    "${currentBuild.currentResult}" == 'SUCCESS'
                }
            }
            options {
                lock("Deploy Cloudformation Infrastructure to Test")
            }
            environment {
                STACK_NAME="channels-email-sample-project-supplemental-infrastructure"
                STACK_FILE="cloudformation/supplemental-infrastructure.yml"
                AWS_DEFAULT_REGION="us-west-2"
            }
            steps {
                withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
                    withAWS(role:'pipeline-to-channels-email-sampleproject-deploy', roleAccount:"$test_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'test-sample-project-infra-pipeline'){
                        setupVersionAndScripts()
                        deployCf()
                    }
                }
            }
        }
        stage("Deploy to Kubernetes Cluster to Test") {
            when {
                branch "master"
                expression {
                    "${currentBuild.currentResult}" == 'SUCCESS'
                }
            }
            options {
                lock("Deploy to Kubernetes Cluster to Test")
            }
            environment {
                POD_SERVICE_ROLE="to-channels-email-sample-project-role"
                ASPNETCORE_ENVIRONMENT="Test"
                AWS_ACCOUNT_ID="${test_aws_account_id}"
                AWS_DEFAULT_REGION="us-west-2"
                EXTERNAL_URL="sample-project-na1.test.inucn.com"
                MIN_REPLICAS="2"
                MAX_REPLICAS="4"
            }
            steps {
                withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
                    withAWS(role:'pipeline-to-channels-email-sampleproject-deploy', roleAccount:"$test_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'test-sample-project-pipeline'){
                        setupVersionAndScripts()
                        deployK8s()
                    }
                }
            }
        }
        // stage("Run Automation IC-Test") {
        //     when {
        //         branch "master"
        //         not {
        //             changeRequest()
        //         }
        //         expression {
        //             "${currentBuild.currentResult}" == 'SUCCESS'
        //         }
        //     }
        //     environment {
        //         AWS_DEFAULT_REGION="us-west-2"
        //         POD_SERVICE_ROLE="to-channels-email-sample-project-role"
        //         ASPNETCORE_ENVIRONMENT="Test"
        //         AWS_ACCOUNT_ID="${test_aws_account_id}"
        //         TEST_RESULTS_XML_BODY=" "
        //     }
        //     steps {
        //         withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
        //             withAWS(role:'pipeline-to-channels-email-sampleproject-deploy', roleAccount:"$test_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'test-sample-project-automation-pipeline'){
        //                 setupVersionAndScripts()
        //                 runTestsK8s('k8s/sample-project-acceptance-testing.yml.template', 'sample-project-acceptance')
        //             }
        //         }
        //     }
        // }
        stage("Build Automation Docker Staging") {
            when {
                branch "master"
            }
            steps {
                setupVersionAndScripts()
                buildDocker("channels-email-sample-project-automation", "-f src/CloudEmail.SampleProject.API.Automation/Dockerfile --pull . --build-arg ASPNETCORE_ENVIRONMENT=Staging")
            }
            post("Push Automation to ECR Staging") {
                success {
                    withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
                        withAWS(role:'pipeline-so-channels-email-sampleproject-deploy', roleAccount:"$staging_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'staging-push-sample-project-automation-pipeline'){
                            pushDocker("545209810301.dkr.ecr.us-west-2.amazonaws.com", "channels-email-sample-project-automation", "rc", true)
                        }
                    }
                }
            }
        }
        stage("Deploy Cloudformation Infrastructure to Staging") {
            when {
                branch "master"
                expression {
                    "${currentBuild.currentResult}" == 'SUCCESS'
                }
            }
            options {
                lock("Deploy Cloudformation Infrastructure to Staging")
            }
            environment {
                STACK_NAME="channels-email-sample-project-supplemental-infrastructure"
                STACK_FILE="cloudformation/supplemental-infrastructure.yml"
                AWS_DEFAULT_REGION="us-west-2"
            }
            steps {
                withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
                    withAWS(role:'pipeline-so-channels-email-sampleproject-deploy', roleAccount:"$staging_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'staging-sample-project-infra-pipeline'){
                        setupVersionAndScripts()
                        deployCf()
                    }
                }
            }
        }
        stage("Deploy to Kubernetes Cluster to Staging") {
            when {
                branch "master"
                expression {
                    "${currentBuild.currentResult}" == 'SUCCESS'
                }
            }
            options {
                lock("Deploy to Kubernetes Cluster to Staging")
            }
            environment {
                POD_SERVICE_ROLE="so-channels-email-sample-project-role"
                ASPNETCORE_ENVIRONMENT="Staging"
                AWS_ACCOUNT_ID="${staging_aws_account_id}"
                AWS_DEFAULT_REGION="us-west-2"
                EXTERNAL_URL="sample-project-na1.staging.inucn.com"
                MIN_REPLICAS="2"
                MAX_REPLICAS="4"
            }
            steps {
                withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
                    withAWS(role:'pipeline-so-channels-email-sampleproject-deploy', roleAccount:"$staging_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'staging-sample-project-pipeline'){
                        setupVersionAndScripts()
                        deployK8s()
                    }
                }
            }
        }
        stage("Run Automation IC-Staging") {
            when {
                branch "master"
                not {
                    changeRequest()
                }
                expression {
                    "${currentBuild.currentResult}" == 'SUCCESS'
                }
            }
            environment {
                AWS_DEFAULT_REGION="us-west-2"
                POD_SERVICE_ROLE="so-channels-email-sample-project-role"
                ASPNETCORE_ENVIRONMENT="Staging"
                AWS_ACCOUNT_ID="${staging_aws_account_id}"
                TEST_RESULTS_XML_BODY=" "
            }
            steps {
                withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
                    withAWS(role:'pipeline-so-channels-email-sampleproject-deploy', roleAccount:"$staging_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'staging-sample-project-automation-pipeline'){
                        setupVersionAndScripts()
                        runTestsK8s('k8s/sample-project-acceptance-testing.yml.template', 'sample-project-acceptance')
                    }
                }
            }
        }
        stage("Deploy to IC-Prod K8") {
            when {
                allOf {
                    branch 'master'
                    expression {
                        "${currentBuild.currentResult}" == 'SUCCESS'
                    }
                }
            }
            parallel {
                stage("Deploy to Oregon") {
                    options {
                        lock("deploy email api to prod oregon k8")
                    }
                    environment {
                        POD_SERVICE_ROLE="ao-channels-email-sample-project-role"
                        ASPNETCORE_ENVIRONMENT="Production"
                        AWS_ACCOUNT_ID="${prod_aws_account_id}"
                        STACK_NAME="channels-email-sample-project-supplemental-infrastructure"
                        STACK_FILE="cloudformation/supplemental-infrastructure.yml"
                        AWS_DEFAULT_REGION="us-west-2"
                        EXTERNAL_URL="sample-project-na1.inucn.com"
                        MIN_REPLICAS="8"
                        MAX_REPLICAS="30"
                    }
                    steps {
                        withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
                            withAWS(role:'pipeline-ao-channels-email-sampleproject-deploy', roleAccount:"$prod_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'oregon-sample-project-pipeline'){
                                deployToProdWithInput("Deploy to Oregon?")
                            }
                        }
                    }
                }
                stage("Deploy to Frankfurt") {
                    options {
                        lock("deploy email api to prod frankfurt k8")
                    }
                    environment {
                        POD_SERVICE_ROLE="af-channels-email-sample-project-role"
                        ASPNETCORE_ENVIRONMENT="Frankfurt"
                        AWS_ACCOUNT_ID="${prod_aws_account_id}"
                        STACK_NAME="channels-email-sample-project-supplemental-infrastructure"
                        STACK_FILE="cloudformation/supplemental-infrastructure.yml"
                        AWS_DEFAULT_REGION="eu-central-1"
                        EXTERNAL_URL="sample-project-eur.inucn.com"
                        MIN_REPLICAS="2"
                        MAX_REPLICAS="8"
                    }
                    steps {
                        withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
                            withAWS(role:'pipeline-af-channels-email-sampleproject-deploy', roleAccount:"$prod_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'frankfurt-sample-project-pipeline'){
                                deployToProdWithInput("Deploy to Frankfurt?")
                            }
                        }
                    }
                }
                stage("Deploy to Sydney") {
                    options {
                        lock("deploy email api to prod sydney k8")
                    }
                    environment {
                        POD_SERVICE_ROLE="aa-channels-email-sample-project-role"
                        ASPNETCORE_ENVIRONMENT="Sydney"
                        AWS_ACCOUNT_ID="${prod_aws_account_id}"
                        STACK_NAME="channels-email-sample-project-supplemental-infrastructure"
                        STACK_FILE="cloudformation/supplemental-infrastructure.yml"
                        AWS_DEFAULT_REGION="ap-southeast-2"
                        EXTERNAL_URL="sample-project-aus.inucn.com"
                        MIN_REPLICAS="2"
                        MAX_REPLICAS="8"
                    }
                    steps {
                        withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
                            withAWS(role:'pipeline-aa-channels-email-sampleproject-deploy', roleAccount:"$prod_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'sydney-sample-project-pipeline'){
                                deployToProdWithInput("Deploy to Sydney?")
                            }
                        }
                    }
                }
                stage("Deploy to London") {
                    options {
                        lock("deploy email api to prod london k8")
                    }
                    environment {
                        POD_SERVICE_ROLE="al-channels-email-sample-project-role"
                        ASPNETCORE_ENVIRONMENT="London"
                        AWS_ACCOUNT_ID="${prod_aws_account_id}"
                        STACK_NAME="channels-email-sample-project-supplemental-infrastructure"
                        STACK_FILE="cloudformation/supplemental-infrastructure.yml"
                        AWS_DEFAULT_REGION="eu-west-2"
                        EXTERNAL_URL="sample-project-uk1.inucn.com"
                        MIN_REPLICAS="2"
                        MAX_REPLICAS="8"
                    }
                    steps {
                        withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
                            withAWS(role:'pipeline-al-channels-email-sampleproject-deploy', roleAccount:"$prod_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'london-sample-project-pipeline'){
                                deployToProdWithInput("Deploy to London?")
                            }
                        }
                    }
                }
                stage("Deploy to Montreal") {
                    options {
                        lock("deploy email api to prod montreal k8")
                    }
                    environment {
                        POD_SERVICE_ROLE="am-channels-email-sample-project-role"
                        ASPNETCORE_ENVIRONMENT="Montreal"
                        AWS_ACCOUNT_ID="${prod_aws_account_id}"
                        STACK_NAME="channels-email-sample-project-supplemental-infrastructure"
                        STACK_FILE="cloudformation/supplemental-infrastructure.yml"
                        AWS_DEFAULT_REGION="ca-central-1"
                        EXTERNAL_URL="sample-project-ca1.inucn.com"
                        MIN_REPLICAS="2"
                        MAX_REPLICAS="8"
                    }
                    steps {
                        withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
                            withAWS(role:'pipeline-am-channels-email-sampleproject-deploy', roleAccount:"$prod_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'montreal-sample-project-pipeline'){
                                deployToProdWithInput("Deploy to Montreal?")
                            }
                        }
                    }
                }
                stage("Deploy to Tokyo") {
                    options {
                        lock("deploy email api to prod tokyo k8")
                    }
                    environment {
                        POD_SERVICE_ROLE="aj-channels-email-sample-project-role"
                        ASPNETCORE_ENVIRONMENT="Tokyo"
                        AWS_ACCOUNT_ID="${prod_aws_account_id}"
                        STACK_NAME="channels-email-sample-project-supplemental-infrastructure"
                        STACK_FILE="cloudformation/supplemental-infrastructure.yml"
                        AWS_DEFAULT_REGION="ap-northeast-1"
                        EXTERNAL_URL="sample-project-jp1.inucn.com"
                        MIN_REPLICAS="2"
                        MAX_REPLICAS="8"
                    }
                    steps {
                        withCredentials([ string(credentialsId: 'channels-email_sample_project _apiPipeline_ExternalId', variable: 'EXTERNAL_ID') ]){
                            withAWS(role:'pipeline-aj-channels-email-sampleproject-deploy', roleAccount:"$prod_aws_account_id", externalId: "$EXTERNAL_ID", duration: 3600, roleSessionName: 'tokyo-sample-project-pipeline'){
                                deployToProdWithInput("Deploy to Tokyo?")
                            }
                        }
                    }
                }
            }
        }
    }
    post {
        failure {
            notifyBuildFailure()
        }
        fixed {
            notifyBuildFixed()
        }
    }
}

def deployToProdWithInput(inputMessage) {
    script {
        try {
            input(inputMessage)
        } catch (err) {
            return
        }
        setupVersionAndScripts()
        deployCf()
        deployK8s()
    }
}
def prepareGithubHub() {
    dir("github-tool") {
        sh """
            rm -rf hub-linux-amd64-2.6.0.tgz
            curl -LJO https://github.com/github/hub/releases/download/v2.6.0/hub-linux-amd64-2.6.0.tgz
            rm -rf hub
            tar -xvf *.tgz -C . && mv hub-linux-amd64*/bin/* /usr/local/bin
            if [ \$? != 0 ]; then
                echo "Error: extraction of github-tool failed!"
                exit 1;
            fi
        """
    }
}
def createPullRequest() {
    if (env.BRANCH_NAME == 'develop') {
        echo "Creating pull request to master."
        prepareGithubHub()
        sh "scripts/jenkins/create-pull-request.sh"
    }
}
def rollback() {
    office365ConnectorSend(
        message:"${env.JOB_NAME} #${env.BUILD_NUMBER} failed! Awaiting input for rollback. (<a href=\"${env.RUN_DISPLAY_URL}\">Open</a>)",
        color: "8B0000",
        status: "FAILED",
        webhookUrl: "${env.BUILD_NOTIFY_WEBHOOK}"
    )
    script {
        def versions = sh(script: 'scripts/cd/tasks/get-eb-versions.sh', returnStdout: true)
        def versionToRollBackTo = input message: 'DEPLOYMENT FAILED - BEGIN ROLLBACK PROCEDURE',
            parameters: [choice(name: 'ElasticBeanstalk Versions', choices: "${versions}", description: 'Please select a target rollback version:')]
        env.TARGET_VERSION = "${versionToRollBackTo}"
        sh(script: 'scripts/cd/tasks/update-eb-to-version.sh', returnStdout: true)
    }
}
def deployCf() {
    sh "scripts/cd/tasks/deploy-cf-template.sh ${PROJECT_VERSION}"
}
def runTestsK8s(templateFile, jobName) {
    sh """
        scripts/cd/tasks/replace-in-yaml.sh \
            --file='${templateFile}' \
            --replacements='{{AWS_ACCOUNT_ID}}=${AWS_ACCOUNT_ID} {{POD_SERVICE_ROLE}}=${POD_SERVICE_ROLE} {{PROJECT_VERSION}}=${PROJECT_VERSION} {{ASPNETCORE_ENVIRONMENT}}=${ASPNETCORE_ENVIRONMENT}'
    """
    sh """
        scripts/kubernetes/kubectl/apply-job-wait-complete.sh \
            --file='${templateFile}' \
            --cluster_name='channels-email' \
            --job_name='${jobName}' \
            --namespace='testing'
    """
    sh "kubectl -n testing -c ${jobName} logs job/${jobName} > TestResults.xml"
    sh """
        scripts/kubernetes/kubectl/delete-manifest.sh --file='${templateFile}' --cluster_name='channels-email'
    """
    sh "cat TestResults.xml"
    xunit(thresholds: [failed(failureNewThreshold: '0', failureThreshold: '0', unstableNewThreshold: '0', unstableThreshold: '0')], tools: [MSTest(deleteOutputFiles: true, failIfNotNew: true, pattern: "TestResults.xml", skipNoTestFiles: false, stopProcessingIfError: true)])
}
def deployK8s() {
    sh """
        scripts/cd/tasks/replace-in-yaml.sh \
            --file='k8s/sample-project-deployment.yml.template' \
            --replacements='{{AWS_ACCOUNT_ID}}=${AWS_ACCOUNT_ID} {{POD_SERVICE_ROLE}}=${POD_SERVICE_ROLE} {{PROJECT_VERSION}}=${PROJECT_VERSION} {{ASPNETCORE_ENVIRONMENT}}=${ASPNETCORE_ENVIRONMENT} {{MIN_REPLICAS}}=${MIN_REPLICAS} {{MAX_REPLICAS}}=${MAX_REPLICAS}'
    """
    sh """
        scripts/kubernetes/kubectl/apply-deployment-with-rollback.sh \
            --file='k8s/sample-project-deployment.yml.template' \
            --namespace='outbound' \
            --deployment_name='sample-project' \
            --cluster_name='channels-email' \
    """
    sh """
        scripts/kubernetes/kubectl/apply-generic.sh \
            --file='k8s/sample-project-service.yml' \
            --cluster_name='channels-email'
    """
}
def buildForMyget() {
    dir("src") {
        sh "dotnet build --configfile .nuget/NuGet.Config /p:Version=${PROJECT_VERSION}"
    }
}
def publishToMyget(projectName, packageName) {
    dir("src") {
        sh "dotnet nuget push ${projectName}/bin/Debug/${packageName}.${PROJECT_VERSION}.nupkg -k $NUGET_KEY -s $NUGET_SOURCE"
    }
}
def buildDocker(imageName, buildArgs) {
    script {
        docker.build(imageName, buildArgs)
    }
}
def pushDocker(registry, imageName, tag, isLatest) {
    script {
        def login = ecrLogin()
        sh "${login}"
        sh "docker tag ${imageName} ${registry}/${imageName}:${PROJECT_VERSION}"
        sh "docker push ${registry}/${imageName}:${PROJECT_VERSION}"

        if (isLatest) {
            sh "docker tag ${imageName} ${registry}/${imageName}:latest"
            sh "docker push ${registry}/${imageName}:latest"
        }

        if (tag != "") {
            sh "docker tag ${imageName} ${registry}/${imageName}:${tag}"
            sh "docker push ${registry}/${imageName}:${tag}"
        }
    }
}
def notifyBuildFailure() {
    office365ConnectorSend(
        message:"${env.JOB_NAME} #${env.BUILD_NUMBER} failed! (<a href=\"${env.RUN_DISPLAY_URL}\">Open</a>)",
        color: "8B0000",
        status: "FAILED",
        webhookUrl: "${env.BUILD_NOTIFY_WEBHOOK}"
    )
}
def notifyBuildFixed() {
    office365ConnectorSend(
        message:"${env.JOB_NAME} #${env.BUILD_NUMBER} has been fixed! (<a href=\"${env.RUN_DISPLAY_URL}\">Open</a>)",
        color: "228B22",
        status: "SUCCESS",
        webhookUrl: "${env.BUILD_NOTIFY_WEBHOOK}"
    )
}
def waitForQualityGateResults() {
    setupWaitForResults()
    sh "scripts/ci/tasks/wait-sonar-results.sh"
}
def runQualityGate() {
    setupSonarResults()
    sh "scripts/jenkins/sonar-upload-dotnet.sh"
    xunit(thresholds: [failed(failureNewThreshold: '0', failureThreshold: '0', unstableNewThreshold: '0', unstableThreshold: '0')],
        tools: [MSTest(deleteOutputFiles: true, failIfNotNew: true, pattern: "CloudEmail.SampleProject.API.Tests-TestResults*.xml", skipNoTestFiles: false, stopProcessingIfError: true)])
}
def setGitVersion() {
    script {
        PROJECT_VERSION=sh(script: "mono /opt/GitVersion/GitVersion.exe /b ${BRANCH_NAME} /showvariable SemVer", returnStdout: true).trim()
        PROJECT_VERSION_NO_TAG=sh(script: "mono /opt/GitVersion/GitVersion.exe /b ${BRANCH_NAME} /showvariable MajorMinorPatch", returnStdout: true).trim()
    }
}
def setupVersionAndScripts() {
    setGitVersion()
    acquireScripts()
    setupVersion()
    echo "setGitVersion, acquireScripts and setupVersion complete."
}
def acquireScripts () {
    sh """
        if [ -d "scripts" ]; then
            cd scripts && git fetch --all && git reset --hard origin/master && find . -type f -iname "*.sh" -exec chmod +x {} \\;
        else
            git clone ${SCRIPTS_REPO_URI_WTOKEN} scripts -b master
            cd scripts && find . -type f -iname "*.sh" -exec chmod +x {} \\;
        fi
    """
    echo "Scripts cloned and made executable."
}
def githubRelease () {
    prepareGithubHub()
    if ("${BRANCH_NAME}" == "master") {
        gitRelease()
    } else {
        gitPreRelease()
    }
}
def gitRelease () {
    sh """
        hub release create -m "Release ${PROJECT_VERSION}" ${PROJECT_VERSION} -t ${BRANCH_NAME}
    """
    echo "Git create release ${PROJECT_VERSION}."
}
def gitPreRelease () {
    sh """
        hub release create -p -m "Pre-Release ${PROJECT_VERSION}" ${PROJECT_VERSION} -t ${BRANCH_NAME}
    """
    echo "Git create pre release ${PROJECT_VERSION}."
}
def setupSonarResults () {
    sh """
        if [ ! -d "sonar" ]; then
            mkdir sonar
        fi
    """
}
def setupVersion () {
    sh """
        if [ ! -d "version" ]; then
            mkdir version
        fi
    """
    echo "PROJECT_VERSION: ${PROJECT_VERSION}"
    echo "PROJECT_VERSION_NO_TAG: ${PROJECT_VERSION_NO_TAG}"
    sh "echo ${PROJECT_VERSION} > version/number"
}
def setupWaitForResults () {
    sh """
        if [ ! -d "s3-sonar-report" ]; then
            mkdir s3-sonar-report
        fi
        mv sonar/* s3-sonar-report/
    """
}
def generateXUnitTestResults() {
	dir("src") {
        
        sh "dotnet test --test-adapter-path:. --logger:xunit CloudEmail.SampleProject.API.Tests"
    }
}

def publishTestResultsToJira() {
    withCredentials([usernamePassword(credentialsId: 'DFChannels_JIRA_ACCESS', passwordVariable: 'JIRA_CLIENT_SECRET', usernameVariable: 'JIRA_CLIENT_ID')]) {
        def tokenResponse = sh(script: "curl -k -v -X POST -d 'client_id=${JIRA_CLIENT_ID}&client_secret=${JIRA_CLIENT_SECRET}&grant_type=client_credentials&scope=jira_api' https://extservice.nice.com:1443/nice/external-prod/oauth-end/oauth2/token",returnStdout: true).trim()
        def tokenId = sh(script: "echo $tokenResponse|grep -Po '(?<=access_token:)[^,]+'", returnStdout: true).trim()
        publishTestResultsToJiraFor("src/CloudEmail.SampleProject.API.Tests/TestResults/TestResults", "DE-25560", tokenId)
    }
}
def publishTestResultsToJiraFor(testFile, testPlan, tokenId) {
    echo "${tokenId}"
    echo "${testFile}"
    echo "${testPlan}"
    def fileName = "${testFile}_${env.BUILD_DATETIME}_${env.BRANCH_NAME}"
    sh (
        script: "echo \"jenkins\" | cp ${testFile}.xml ${fileName}.xml"
    )
    sh """
        cat "${fileName}.xml"
    """
    def testExecutionCreated = sh(script: "curl -X POST -H 'Content-Type: multipart/form-data' -F 'file=@${fileName}.xml' -H 'Authorization: Bearer ${tokenId}' -k 'https://extservice.nice.com:1443/nice/external-prod/rest/raven/1.0/import/execution/xunit?projectKey=DE&testPlanKey=${testPlan}'", returnStdout: true).trim()
    echo "${testExecutionCreated}"
}