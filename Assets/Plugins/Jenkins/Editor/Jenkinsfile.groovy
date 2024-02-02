@Library('unity-build-jenkinslib') _
import unity.build.*

def iOSBuilder
def AndroidBuilder

boolean isBuildTargetIOS()
{
    return params.BUILD_TARGET.contains('Both') || params.BUILD_TARGET.contains('iOS')
}

boolean isBuildTargetAndroid()
{
    return params.BUILD_TARGET.contains('Both') || params.BUILD_TARGET.contains('Android')
}

pipeline {
    agent any
    environment {
        RESET_DIRS = "Library, obj, Build, Logs, Temp"
    }
    options {
        timestamps()
        timeout(time: 2, unit: 'HOURS')
    }
    parameters {
        choice(name: 'BUILD_TARGET', choices: ['Both', 'iOS', 'Android'], description: 'ビルド対象')
        booleanParam(name: 'DEVELOPMENT', defaultValue: true, description: 'Development build')
        // iOS
        choice(name: 'CLEAN_BUILD', choices: ['Not Selected', 'Clean Cache', 'Project Reset'], description: 'Clean Cache : キャッシュファイルを削除する \n Project Reset : Libralyフォルダなどを削除し再構築する')
        booleanParam(name: 'SEND_TO_SERVER', defaultValue: true, description: 'サーバーに送信するか？ (Ad hoc or App Store)')
        credentials(name: 'PROVISIONING_PROFILE', credentialType: 'Secret File', defaultValue: 'WildcardAdHoc', description: 'Provisioning profileを指定する。Ad Hoc内に', required: true)
        // Android
        booleanParam(name: 'GRADLE_USE_EMBEDDED', defaultValue: true, description: 'Unityに含まれるGradleを使う')
        choice(name: 'ANDROID_EXPORT_TYPE', choices: ['Package', 'App Bundle', 'Studio Project'], description: 'Package : Android Package(.apk)の出力\nApp Bundle : Android App Bundl(.aab)の出力\nStudio Project : Android Studioで起動できるプロジェクトの出力')
        credentials(name: 'KEYSTORE', credentialType: 'Secret File', defaultValue: '', description: 'Keystoreを設定', required: false)
        credentials(name: 'KEYSTORE_PASS', credentialType: 'Secret Text', defaultValue: '', description: 'Keystoreのパスワード', required: false)
        credentials(name: 'KEYSTORE_ALIAS', credentialType: 'Username with password', defaultValue: 'Keystoreのalias、aliasNameとaliasPass', description: '', required: false)
        // Slack
        credentials(name: 'SLACK_TOKEN', credentialType: 'Secret Text', defaultValue: 'Slack_MergeGames', description: 'Slackのトークンを指定')
        string(name: 'SLACK_CHANNEL', defaultValue: '自動ビルドテスト', description: '送信チャンネル')
    }
    stages {
        stage('Initialize') {
            steps {
                echo 'Initialize start'
                script {
                    if(isBuildTargetIOS())
                    {
                        iOSBuilder = new iOSBuilder(params, this)
                        RESET_DIRS += ", ${iOSBuilder.BUILD_PATH}"
                        RESET_DIRS += ", ${iOSBuilder.DIST_PATH}"
                    }
                    if(isBuildTargetAndroid())
                    {
                        AndroidBuilder = new AndroidBuilder(params, this)
                        RESET_DIRS += ", ${AndroidBuilder.BUILD_PATH}"
                        RESET_DIRS += ", ${AndroidBuilder.DIST_PATH}"
                    }
                }
            }
        }
        stage('Project Reset') {
            when {
                expression {
                    return params.CLEAN_BUILD.contains('Project Reset')
                }
            }
            steps {
                script {
                    echo 'Project Reset start'
                    def dirsArr = RESET_DIRS.split(',')
                    dirsArr.each { dir ->
                        sh "rm -rf ${dir.trim()}"
                    }
                }
            }
        }
        stage('Build Project(iOS)')
        {
            when {
                expression {
                    return isBuildTargetIOS()
                }
            }
            steps {
                script {
                    iOSBuilder.buildProject()
                }
            }
        }
        stage('Build Project(Android)')
        {
            when {
                expression {
                    return isBuildTargetAndroid()
                }
            }
            steps {
                script {
                    AndroidBuilder.buildProject()
                }
            }
        }
        stage('Xcode Archive') {
            when {
                expression {
                    return isBuildTargetIOS()
                }
            }
            steps {
                script {
                    iOSBuilder.buildAchieve()
                }
            }
        }
        stage('Xcode ipa') {
            when {
                expression {
                    return isBuildTargetIOS()
                }
            }
            steps {
                script {
                    iOSBuilder.buildIpa("${GLOBAL_FILES_IOS}ExportOptions.plist")
                }
            }
        }
        stage('iOS validate aap') {
            when {
                expression {
                    return isBuildTargetIOS() && iOSBuilder.canISendAppStore()
                }
            }
            steps {
                script {
                    iOSBuilder.validateApp()
                }
            }
        }
        stage('Send to server') {
            when {
                expression {
                    return iOSBuilder.canISendServerForAdHoc() || AndroidBuilder.canISendServer()
                }
            }
            parallel {
                stage('iOS-Adhoc') {
                    when {
                        expression {
                            return isBuildTargetIOS() && iOSBuilder.canISendServerForAdHoc()
                        }
                    }
                    steps {
                        script {
                            iOSBuilder.UploadIpa("${SHARE_TEMPLATE_PATH}index.html", "${SHARE_TEMPLATE_PATH}ipa.plist")
                        }
                    }
                }
                stage('iOS-App Store') {
                    when {
                        expression {
                            return isBuildTargetIOS() && iOSBuilder.canISendAppStore()
                        }
                    }
                    steps {
                        script {
                            iOSBuilder.sendToStore()
                        }
                    }
                }
                stage('Android-Apk') {
                    when {
                        expression {
                            return isBuildTargetAndroid() && AndroidBuilder.canISendServer()
                        }
                    }
                    steps {
                        script {
                            AndroidBuilder.sendToServer("${SHARE_TEMPLATE_PATH}index.html", DEFAULT_APP_ICON_PATH)
                        }
                    }
                }
                stage('Android-Aab') {
                    when {
                        expression {
                            return isBuildTargetAndroid() && AndroidBuilder.isExportAab()
                        }
                    }
                    steps {
                        script {
                            AndroidBuilder.generateApkFromAab()
                        }
                    }
                }
            }
        }
    }
    post{
        success{
            echo "Success"

            script {
                if(isBuildTargetIOS())
                {
                    archiveArtifacts artifacts: "${iOSBuilder.IPA_PATH}/**/*", fingerprint: true, followSymlinks: false
                    archiveArtifacts artifacts: "${iOSBuilder.BUILD_SNAPSHOT_PATH}", fingerprint: true, followSymlinks: false
                }

                if(isBuildTargetAndroid())
                {
                    archiveArtifacts artifacts: "${AndroidBuilder.DIST_APP_PATH}/*", fingerprint: true, followSymlinks: false
                    archiveArtifacts artifacts: AndroidBuilder.BUILD_SNAPSHOT_PATH, fingerprint: true, followSymlinks: false
                }

                if(params.SLACK_TOKEN
                   && params.SLACK_CHANNEL)
                {
                    if(isBuildTargetIOS())
                    {
                        iOSBuilder.sendSuccessToSlack(SLACK_TOKEN, SLACK_CHANNEL)
                    }

                    if(isBuildTargetAndroid())
                    {
                        AndroidBuilder.sendSuccessToSlack(SLACK_TOKEN, SLACK_CHANNEL)
                    }
                }
            }
        }
        failure{
            echo "Failed"

            script {
                if(params.SLACK_TOKEN
                   && params.SLACK_CHANNEL)
                {
                    if(isBuildTargetIOS())
                    {
                        iOSBuilder.sendFailureToSlack(SLACK_TOKEN, SLACK_CHANNEL)
                    }

                    if(isBuildTargetAndroid())
                    {
                        AndroidBuilder.sendFailureToSlack(SLACK_TOKEN, SLACK_CHANNEL)
                    }
                }
            }
        }
    }
}