<wsHttpBinding>
				<binding name="wsHttpEndpointBinding">
					<security mode="Message">
						<!-- negotialServiceCredential=false means client is not possession of the cert - private portion of it-->
						<message clientCredentialType="Certificate" negotiateServiceCredential="false" />
					</security>
				</binding>
</wsHttpBinding>
